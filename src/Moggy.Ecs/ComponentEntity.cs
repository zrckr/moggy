namespace Moggy.Ecs;

/// <summary>
/// Couples an entity with its component value.
/// </summary>
internal struct ComponentEntity<T> where T : struct
{
    public Entity Entity;

    public T Component;

    public ComponentEntity(Entity entity, in T component)
    {
        Entity = entity;
        Component = component;
    }
}
