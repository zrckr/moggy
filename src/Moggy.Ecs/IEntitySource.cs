namespace Moggy.Ecs;

/// <summary>
/// Exposes indexed entity membership.
/// </summary>
internal interface IEntitySource
{
    int Count { get; }

    Entity EntityAt(int index);

    bool Contains(Entity entity);
}
