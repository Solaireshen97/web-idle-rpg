namespace Shared.Players;

public sealed record DungeonWaveDefinitionDto(
    int WaveIndex,
    IReadOnlyList<string> EnemyKeys);
