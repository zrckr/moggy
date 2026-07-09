namespace Moggy.Ecs;

/// <summary>
/// Owns entities, component storage, and query creation.
/// </summary>
public sealed class Registry
{
    /// <summary>
    /// Gets the structural version of the registry.
    /// </summary>
    public int Version { get; private set; }

    /// <summary>
    /// Gets the number of live entities.
    /// </summary>
    public int Size => _entities.Count;

    private readonly EntityStorage _entities = new();

    private readonly Dictionary<Type, IComponentStorage> _storages = new();

    private ulong _nextId = 1;

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    /// <remarks>
    /// Entity handles are valid only for the Registry that created them.
    /// </remarks>
    public Entity Create()
    {
        if (_nextId == 0)
        {
            throw new InvalidOperationException("Entity ID space exhausted.");
        }

        var entity = new Entity(_nextId++);
        _entities.Add(entity);
        Version += 1;
        return entity;
    }

    /// <summary>
    /// Returns whether the entity is still alive.
    /// </summary>
    public bool IsAlive(Entity entity)
    {
        return _entities.Contains(entity);
    }

    /// <summary>
    /// Destroys an entity and removes all of its components.
    /// </summary>
    public bool Destroy(Entity entity)
    {
        if (!_entities.Remove(entity))
        {
            return false;
        }

        foreach (var storage in _storages.Values)
        {
            storage.Remove(entity);
        }

        Version += 1;
        return true;
    }

    /// <summary>
    /// Returns whether the entity has a component of type <typeparamref name="T"/>.
    /// </summary>
    public bool Has<T>(Entity entity) where T : struct
    {
        return IsAlive(entity) && GetOrCreateStorage<T>().Contains(entity);
    }

    /// <summary>
    /// Returns a reference to the entity's component of type <typeparamref name="T"/>.
    /// </summary>
    public ref T Get<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        if (IsTag<T>())
        {
            throw new InvalidOperationException($"Tag component {typeof(T)} has no value to retrieve.");
        }

        var componentStorage = (ComponentStorage<T>)GetOrCreateStorage<T>();
        return ref componentStorage.Get(entity);
    }

    /// <summary>
    /// Adds or replaces a component of type <typeparamref name="T"/> on an entity.
    /// </summary>
    public void Set<T>(Entity entity, in T component) where T : struct
    {
        EnsureAlive(entity);
        if (IsTag<T>())
        {
            var tagStore = (TagStorage<T>)GetOrCreateStorage<T>();
            if (tagStore.Add(entity))
            {
                Version++;
            }

            return;
        }

        var componentStorage = (ComponentStorage<T>)GetOrCreateStorage<T>();
        if (componentStorage.Set(entity, component))
        {
            Version++;
        }
    }

    /// <summary>
    /// Removes a component of type <typeparamref name="T"/> from an entity.
    /// </summary>
    public bool Remove<T>(Entity entity) where T : struct
    {
        if (!IsAlive(entity))
        {
            return false;
        }

        if (GetOrCreateStorage<T>().Remove(entity))
        {
            Version++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Starts building a query against this registry.
    /// </summary>
    public QueryBuilder Query()
    {
        return new QueryBuilder(this);
    }

    internal Query BuildQuery(
        IReadOnlyList<IComponentStorage> included,
        IReadOnlyList<IComponentStorage> excluded)
    {
        IEntitySource candidates = _entities;
        foreach (var storage in included)
        {
            if (storage.Count < candidates.Count)
            {
                candidates = storage;
            }
        }

        return new Query(this, candidates, included, excluded);
    }

    internal IComponentStorage GetOrCreateStorage<T>() where T : struct
    {
        var type = typeof(T);
        if (!_storages.TryGetValue(type, out var storage))
        {
            storage = IsTag<T>()
                ? new TagStorage<T>()
                : new ComponentStorage<T>();

            _storages.Add(type, storage);
        }

        return storage;
    }

    private void EnsureAlive(Entity entity)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity.Id} is not alive.");
        }
    }

    private static bool IsTag<T>() where T : struct
    {
        return typeof(ITag).IsAssignableFrom(typeof(T));
    }
}