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

    private List<IDeferredCommand> _deferredCommands = [];

    private int _activeEnumerators;

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
    /// Creates an entity with a component of type <typeparamref name="T1"/>.
    /// </summary>
    public Entity Create<T1>(in T1 component1) where T1 : struct
    {
        var entity = Create();
        Set(entity, component1);
        return entity;
    }

    /// <summary>
    /// Creates an entity with components of types <typeparamref name="T1"/> and
    /// <typeparamref name="T2"/>.
    /// </summary>
    public Entity Create<T1, T2>(in T1 component1, in T2 component2)
        where T1 : struct
        where T2 : struct
    {
        var entity = Create(component1);
        Set(entity, component2);
        return entity;
    }

    /// <summary>
    /// Creates an entity with components of types <typeparamref name="T1"/>,
    /// <typeparamref name="T2"/>, and <typeparamref name="T3"/>.
    /// </summary>
    public Entity Create<T1, T2, T3>(in T1 component1, in T2 component2, in T3 component3)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var entity = Create(component1, component2);
        Set(entity, component3);
        return entity;
    }

    /// <summary>
    /// Creates an entity with components of types <typeparamref name="T1"/>,
    /// <typeparamref name="T2"/>, <typeparamref name="T3"/>, and
    /// <typeparamref name="T4"/>.
    /// </summary>
    public Entity Create<T1, T2, T3, T4>(
        in T1 component1,
        in T2 component2,
        in T3 component3,
        in T4 component4)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
    {
        var entity = Create(component1, component2, component3);
        Set(entity, component4);
        return entity;
    }

    /// <summary>
    /// Creates an entity with components of types <typeparamref name="T1"/> through
    /// <typeparamref name="T5"/>.
    /// </summary>
    public Entity Create<T1, T2, T3, T4, T5>(
        in T1 component1,
        in T2 component2,
        in T3 component3,
        in T4 component4,
        in T5 component5)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
    {
        var entity = Create(component1, component2, component3, component4);
        Set(entity, component5);
        return entity;
    }

    /// <summary>
    /// Creates an entity with components of types <typeparamref name="T1"/> through
    /// <typeparamref name="T6"/>.
    /// </summary>
    public Entity Create<T1, T2, T3, T4, T5, T6>(
        in T1 component1,
        in T2 component2,
        in T3 component3,
        in T4 component4,
        in T5 component5,
        in T6 component6)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
        where T6 : struct
    {
        var entity = Create(component1, component2, component3, component4, component5);
        Set(entity, component6);
        return entity;
    }

    /// <summary>
    /// Creates an entity with components of types <typeparamref name="T1"/> through
    /// <typeparamref name="T7"/>.
    /// </summary>
    public Entity Create<T1, T2, T3, T4, T5, T6, T7>(
        in T1 component1,
        in T2 component2,
        in T3 component3,
        in T4 component4,
        in T5 component5,
        in T6 component6,
        in T7 component7)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
        where T6 : struct
        where T7 : struct
    {
        var entity = Create(component1, component2, component3, component4, component5, component6);
        Set(entity, component7);
        return entity;
    }

    /// <summary>
    /// Creates an entity with components of types <typeparamref name="T1"/> through
    /// <typeparamref name="T8"/>.
    /// </summary>
    public Entity Create<T1, T2, T3, T4, T5, T6, T7, T8>(
        in T1 component1,
        in T2 component2,
        in T3 component3,
        in T4 component4,
        in T5 component5,
        in T6 component6,
        in T7 component7,
        in T8 component8)
        where T1 : struct
        where T2 : struct
        where T3 : struct
        where T4 : struct
        where T5 : struct
        where T6 : struct
        where T7 : struct
        where T8 : struct
    {
        var entity = Create(component1, component2, component3, component4, component5, component6, component7);
        Set(entity, component8);
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
    /// <remarks>
    /// Do not call this method while enumerating a <see cref="Query"/>. Use
    /// <see cref="SetDeferred{T}(Entity, in T)"/> instead.
    /// </remarks>
    public void Set<T>(Entity entity, in T component) where T : struct
    {
        EnsureAlive(entity);
        if (IsTag<T>())
        {
            throw new InvalidOperationException(
                $"Tag component {typeof(T)} cannot be set with {nameof(Set)}. Use {nameof(SetTag)} instead.");
        }

        var componentStorage = (ComponentStorage<T>)GetOrCreateStorage<T>();
        if (componentStorage.Set(entity, component))
        {
            Version++;
        }
    }

    /// <summary>
    /// Adds a tag component of type <typeparamref name="T"/> to an entity.
    /// </summary>
    /// <remarks>
    /// Do not call this method while enumerating a <see cref="Query"/>. Use
    /// <see cref="SetTagDeferred{T}(Entity)"/> instead.
    /// </remarks>
    public void SetTag<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        if (!IsTag<T>())
        {
            throw new InvalidOperationException($"Component {typeof(T)} is not a tag. Use {nameof(Set)} instead.");
        }

        var tagStore = (TagStorage<T>)GetOrCreateStorage<T>();
        if (tagStore.Add(entity))
        {
            Version++;
        }
    }

    /// <summary>
    /// Defers adding or replacing a component of type <typeparamref name="T"/> until
    /// the current query enumeration completes.
    /// </summary>
    public void SetDeferred<T>(Entity entity, in T component) where T : struct
    {
        EnsureAlive(entity);
        if (IsTag<T>())
        {
            throw new InvalidOperationException(
                $"Tag component {typeof(T)} cannot be set with {nameof(SetDeferred)}. Use {nameof(SetTagDeferred)} instead.");
        }

        EnsureEnumerating();
        _deferredCommands.Add(new SetDeferredCommand<T>(entity, component));
    }

    /// <summary>
    /// Defers adding a tag component of type <typeparamref name="T"/> until the
    /// current query enumeration completes.
    /// </summary>
    public void SetTagDeferred<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        if (!IsTag<T>())
        {
            throw new InvalidOperationException(
                $"Component {typeof(T)} is not a tag. Use {nameof(SetDeferred)} instead.");
        }

        EnsureEnumerating();
        _deferredCommands.Add(new SetTagDeferredCommand<T>(entity));
    }

    /// <summary>
    /// Removes a component of type <typeparamref name="T"/> from an entity.
    /// </summary>
    /// <remarks>
    /// Do not call this method while enumerating a <see cref="Query"/>. Use
    /// <see cref="RemoveDeferred{T}(Entity)"/> instead.
    /// </remarks>
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
    /// Defers removing a component of type <typeparamref name="T"/> until the
    /// current query enumeration completes.
    /// </summary>
    public void RemoveDeferred<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        EnsureEnumerating();
        _deferredCommands.Add(new RemoveDeferredCommand<T>(entity));
    }

    /// <summary>
    /// Returns a reference to the only component of type <typeparamref name="T"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no component or more than one component of type
    /// <typeparamref name="T"/> exists.
    /// </exception>
    public ref T Singleton<T>() where T : struct
    {
        if (IsTag<T>())
        {
            throw new InvalidOperationException($"Tag component {typeof(T)} has no value to retrieve.");
        }

        if (!_storages.TryGetValue(typeof(T), out var storage) || storage.Count == 0)
        {
            throw new InvalidOperationException($"No singleton component of type {typeof(T)} exists.");
        }

        if (storage.Count > 1)
        {
            throw new InvalidOperationException($"More than one component of type {typeof(T)} exists.");
        }

        var componentStorage = (ComponentStorage<T>)storage;
        return ref componentStorage.Get(componentStorage.EntityAt(0));
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

    internal void BeginEnumeration()
    {
        _activeEnumerators++;
    }

    internal void EndEnumeration()
    {
        if (_activeEnumerators == 0)
        {
            throw new InvalidOperationException("No active query enumeration to end.");
        }

        _activeEnumerators--;
        if (_activeEnumerators == 0)
        {
            PlaybackDeferred();
        }
    }

    private void EnsureAlive(Entity entity)
    {
        if (!IsAlive(entity))
        {
            throw new InvalidOperationException($"Entity {entity.Id} is not alive.");
        }
    }

    private void EnsureEnumerating()
    {
        if (_activeEnumerators == 0)
        {
            throw new InvalidOperationException(
                "Deferred component changes can only be scheduled while a query is being enumerated.");
        }
    }

    private void PlaybackDeferred()
    {
        if (_deferredCommands.Count != 0)
        {
            var commands = _deferredCommands;
            _deferredCommands = new List<IDeferredCommand>();
            foreach (var command in commands)
            {
                command.Apply(this);
            }
        }
    }

    private static bool IsTag<T>() where T : struct
    {
        return typeof(ITag).IsAssignableFrom(typeof(T));
    }

    private interface IDeferredCommand
    {
        void Apply(Registry registry);
    }

    private readonly struct SetDeferredCommand<T>(Entity entity, T component) : IDeferredCommand where T : struct
    {
        public void Apply(Registry registry)
        {
            registry.Set(entity, component);
        }
    }

    private readonly struct SetTagDeferredCommand<T>(Entity entity) : IDeferredCommand where T : struct
    {
        public void Apply(Registry registry)
        {
            registry.SetTag<T>(entity);
        }
    }

    private readonly struct RemoveDeferredCommand<T>(Entity entity) : IDeferredCommand where T : struct
    {
        public void Apply(Registry registry)
        {
            registry.Remove<T>(entity);
        }
    }
}
