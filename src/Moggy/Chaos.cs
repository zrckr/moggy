namespace Moggy;

public sealed record ChaosProperties
{
    public int AttentionMaxLevel { get; init; }

    public int[] AttentionThresholds { get; init; } = [];

    public int PerBug { get; init; }

    public int PerAbilityRoll { get; init; }

    public int ComboMultiplier { get; init; }

    public int ComboWindowSeconds { get; init; }

    public int DecayDelaySeconds { get; init; }

    public int DecayPerSecond { get; init; }

    public int RemovedPerStomp { get; init; }
}

public struct Chaos()
{
}

public sealed class ChaosSystem : GameSystem
{
}
