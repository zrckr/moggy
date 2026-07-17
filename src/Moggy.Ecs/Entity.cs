namespace Moggy.Ecs;

/// <summary>
/// Identifies an entity managed by a <see cref="Registry"/>.
/// </summary>
public readonly record struct Entity(ulong Id)
{
    /// <summary>
    /// Gets the default invalid entity handle.
    /// </summary>
    public static Entity Invalid => default;

    /// <summary>
    /// Gets whether this handle refers to a non-zero entity id.
    /// </summary>
    public bool IsValid => Id != 0;
}
