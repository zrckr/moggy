namespace Moggy.Ecs;

/// <summary>
/// Defines storage operations for a component type.
/// </summary>
internal interface IComponentStorage : IEntitySource
{
    bool Remove(Entity entity);
}
