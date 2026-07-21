namespace Moggy;

public sealed record BugProperties
{
    public int Count { get; init; }

    public float MovementSpeed { get; init; }

    public float FleeDistance { get; init; }

    public int SpawnSeed { get; init; }

    public string MoveSprite { get; init; } = string.Empty;
}

public struct Bug()
{
}

public sealed class BugSystem : GameSystem
{
}
