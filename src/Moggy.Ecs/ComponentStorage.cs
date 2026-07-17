using System.Runtime.InteropServices;

namespace Moggy.Ecs;

/// <summary>
/// Stores component values for one component type.
/// </summary>
internal sealed class ComponentStorage<T> : IComponentStorage where T : struct
{
    public int Count => _dense.Count;

    private readonly List<ComponentEntity<T>> _dense = new();

    private readonly Dictionary<Entity, int> _sparse = new();

    public Entity EntityAt(int index)
    {
        return _dense[index].Entity;
    }

    public bool Contains(Entity entity)
    {
        return _sparse.ContainsKey(entity);
    }

    public ref T Get(Entity entity)
    {
        return ref CollectionsMarshal.AsSpan(_dense)[_sparse[entity]].Component;
    }

    public bool Set(Entity entity, in T component)
    {
        if (_sparse.TryGetValue(entity, out var index))
        {
            CollectionsMarshal.AsSpan(_dense)[index].Component = component;
            return false;
        }

        _sparse.Add(entity, _dense.Count);
        _dense.Add(new ComponentEntity<T>(entity, component));
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
            var movedComponentEntity = _dense[lastIndex];
            _dense[index] = movedComponentEntity;
            _sparse[movedComponentEntity.Entity] = index;
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