namespace Moggy;

public sealed record LevelDefinition
{
    public int TilingRows { get; init; }

    public int TilingColumns { get; init; }

    public int CellSize { get; init; }
}

public sealed record PlayerDefinition
{
    public float MovementSpeed { get; init; }

    public string IdleSprite { get; init; } = string.Empty;

    public string MoveSprite { get; init; } = string.Empty;
}

public sealed record EnemyDefinition
{
    public int Count { get; init; }

    public float MovementSpeed { get; init; }

    public int SpawnSeed { get; init; }

    public string MoveSprite { get; init; } = string.Empty;
}