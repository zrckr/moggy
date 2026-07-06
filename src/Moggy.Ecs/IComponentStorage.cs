namespace Moggy.Ecs;

/// <summary>
/// Defines storage operations for a component type.
/// </summary>
internal interface IComponentStorage : IEntitySource
{
    Type ComponentType { get; }

    bool Remove(Entity entity);

    void Clear();
}