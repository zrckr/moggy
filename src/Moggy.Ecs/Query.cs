namespace Moggy.Ecs;

/// <summary>
/// Builds and enumerates entities that match a component filter.
/// </summary>
public sealed class Query
{
    private readonly Registry _registry;

    private IEntitySource _candidates;

    private bool _frozen;

    private readonly List<IComponentStorage> _included = new();

    private readonly List<IComponentStorage> _excluded = new();

    internal Query(Registry registry)
    {
        _registry = registry;
        _candidates = registry.Entities;
    }

    /// <summary>
    /// Requires entities to have a component of type <typeparamref name="T"/>.
    /// </summary>
    public Query With<T>() where T : struct
    {
        EnsureMutable();

        var storage = _registry.GetOrCreateStorage<T>();
        if (_excluded.Contains(storage))
        {
            throw new InvalidOperationException($"{typeof(T)} is already excluded.");
        }

        if (_included.Contains(storage))
        {
            return this;
        }

        _included.Add(storage);

        // Scan the smallest included storage to reduce membership checks.
        if (storage.Count < _candidates.Count)
        {
            _candidates = storage;
        }

        return this;
    }

    /// <summary>
    /// Rejects entities that have a component of type <typeparamref name="T"/>.
    /// </summary>
    public Query Without<T>() where T : struct
    {
        EnsureMutable();

        var storage = _registry.GetOrCreateStorage<T>();
        if (_included.Contains(storage))
        {
            throw new InvalidOperationException($"{typeof(T)} is already included.");
        }

        if (!_excluded.Contains(storage))
        {
            _excluded.Add(storage);
        }

        return this;
    }

    /// <summary>
    /// Returns an enumerator over matching entities.
    /// </summary>
    public Enumerator GetEnumerator()
    {
        _frozen = true;
        _registry.BeginEnumeration();
        return new Enumerator(this);
    }

    /// <summary>
    /// Returns the zero-based position of an entity in the query, or -1 when absent.
    /// </summary>
    public int IndexOf(Entity entity)
    {
        var index = 0;
        foreach (var match in this)
        {
            if (match == entity)
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    /// <summary>
    /// Chooses a matching entity uniformly at random.
    /// </summary>
    public Entity Choose(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        var chosen = Entity.Invalid;
        var count = 0;

        foreach (var entity in this)
        {
            count++;
            if (random.Next(count) == 0)
            {
                chosen = entity;
            }
        }

        if (count == 0)
        {
            throw new InvalidOperationException("Query contains no matching entities.");
        }

        return chosen;
    }

    /// <summary>
    /// Returns the first matching entity.
    /// </summary>
    public Entity First()
    {
        var enumerator = GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Query contains no matching entities.");
            }

            return enumerator.Current;
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    /// <summary>
    /// Returns the only matching entity.
    /// </summary>
    public Entity Single()
    {
        var enumerator = GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Query contains no matching entities.");
            }

            var entity = enumerator.Current;
            if (enumerator.MoveNext())
            {
                throw new InvalidOperationException(
                    "Query contains more than one matching entity."
                );
            }

            return entity;
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    /// <summary>
    /// Returns the last matching entity.
    /// </summary>
    public Entity Last()
    {
        var enumerator = GetEnumerator();
        try
        {
            if (!enumerator.MoveNext())
            {
                throw new InvalidOperationException("Query contains no matching entities.");
            }

            var entity = enumerator.Current;
            while (enumerator.MoveNext())
            {
                entity = enumerator.Current;
            }

            return entity;
        }
        finally
        {
            enumerator.Dispose();
        }
    }


    /// <summary>
    /// Returns true is there's any matching entity.
    /// </summary>
    public bool Any()
    {
        var enumerator = GetEnumerator();
        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            enumerator.Dispose();
        }
    }

    /// <summary>
    /// Collects entities into readonly collection.
    /// </summary>
    public IReadOnlyList<Entity> Collect()
    {
        var collected = new List<Entity>();

        foreach (var entity in this)
        {
            collected.Add(entity);
        }

        return collected;
    }

    private void EnsureMutable()
    {
        if (_frozen)
        {
            throw new InvalidOperationException("Query filters cannot change after enumeration!");
        }
    }

    /// <summary>
    /// Iterates matching entities. Immediate structural changes invalidate the
    /// enumeration; deferred changes are applied after it completes.
    /// </summary>
    public struct Enumerator(Query query) : IDisposable
    {
        /// <summary>
        /// Gets the current entity.
        /// </summary>
        public Entity Current { get; private set; } = default;

        private readonly int _version = query._registry.Version;

        private int _index = -1;

        private bool _disposed;

        /// <summary>
        /// Advances to the next matching entity.
        /// </summary>
        public bool MoveNext()
        {
            if (_disposed)
            {
                return false;
            }

            if (_version != query._registry.Version)
            {
                throw new InvalidOperationException("Entity structure changed while a query was being enumerated.");
            }

            while (++_index < query._candidates.Count)
            {
                var entity = query._candidates.EntityAt(_index);
                var matches = true;

                // ReSharper disable once ForCanBeConvertedToForeach
                // ReSharper disable once LoopCanBeConvertedToQuery
                for (var i = 0; i < query._included.Count; i++)
                {
                    if (!query._included[i].Contains(entity))
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches)
                {
                    continue;
                }

                // ReSharper disable once ForCanBeConvertedToForeach
                // ReSharper disable once LoopCanBeConvertedToQuery
                for (var i = 0; i < query._excluded.Count; i++)
                {
                    if (query._excluded[i].Contains(entity))
                    {
                        matches = false;
                        break;
                    }
                }

                if (!matches)
                {
                    continue;
                }

                Current = entity;
                return true;
            }

            Dispose();
            return false;
        }

        /// <summary>
        /// Completes the enumeration and applies any pending structural changes when
        /// this is the last active query enumerator.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            query._registry.EndEnumeration();
        }
    }
}
