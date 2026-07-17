namespace Moggy.Ecs;

/// <summary>
/// Builds entity query from included and excluded component types.
/// </summary>
public sealed class QueryBuilder
{
    private readonly Registry _registry;

    private readonly List<IComponentStorage> _included = new();

    private readonly List<IComponentStorage> _excluded = new();

    private bool _built;

    internal QueryBuilder(Registry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// Requires entities to have a component of type <typeparamref name="T"/>.
    /// </summary>
    public QueryBuilder Include<T>() where T : struct
    {
        EnsureNotBuilt();

        var storage = _registry.GetOrCreateStorage<T>();
        if (_excluded.Contains(storage))
        {
            throw new InvalidOperationException($"{typeof(T)} is already excluded.");
        }

        if (!_included.Contains(storage))
        {
            _included.Add(storage);
        }

        return this;
    }

    /// <summary>
    /// Rejects entities that have a component of type <typeparamref name="T"/>.
    /// </summary>
    public QueryBuilder Exclude<T>() where T : struct
    {
        EnsureNotBuilt();

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
    /// Creates the query.
    /// </summary>
    public Query Build()
    {
        EnsureNotBuilt();
        _built = true;
        return _registry.BuildQuery(_included, _excluded);
    }

    private void EnsureNotBuilt()
    {
        if (_built)
        {
            throw new InvalidOperationException("QueryBuilder has already produced a query.");
        }
    }
}
