namespace Moggy.Ecs;

/// <summary>
/// Stores entity membership for a tag component type.
/// </summary>
internal sealed class TagStorage<T> : IComponentStorage where T : struct
{
    public int Count => _storage.Count;

    private readonly EntityStorage _storage = new();

    public Entity EntityAt(int index)
    {
        return _storage.EntityAt(index);
    }

    public bool Contains(Entity entity)
    {
        return _storage.Contains(entity);
    }

    public bool Add(Entity entity)
    {
        return _storage.Add(entity);
    }

    public bool Remove(Entity entity)
    {
        return _storage.Remove(entity);
    }

    public void Clear()
    {
        _storage.Clear();
    }
}