namespace Moggy.Ecs;

/// <summary>
/// Stores entities in dense-sparse form.
/// </summary>
internal sealed class EntityStorage : IEntitySource
{
    public int Count => _dense.Count;

    private readonly List<Entity> _dense = [];

    private readonly Dictionary<Entity, int> _sparse = [];

    public Entity EntityAt(int index)
    {
        return _dense[index];
    }

    public bool Contains(Entity entity)
    {
        return _sparse.ContainsKey(entity);
    }

    public bool Add(Entity entity)
    {
        if (_sparse.ContainsKey(entity))
        {
            return false;
        }

        _sparse.Add(entity, _dense.Count);
        _dense.Add(entity);
        return true;
    }

    public bool Remove(Entity entity)
    {
        if (!_sparse.Remove(entity, out var index))
        {
            return false;
        }

        var lastIndex = _dense.Count - 1;
        if (index != lastIndex)
        {
            var movedEntity = _dense[lastIndex];
            _dense[index] = movedEntity;
            _sparse[movedEntity] = index;
        }

        _dense.RemoveAt(lastIndex);
        return true;
    }

    public void Clear()
    {
        _dense.Clear();
        _sparse.Clear();
    }
}
