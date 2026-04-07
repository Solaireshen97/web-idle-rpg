namespace Shared.Players;

public sealed record AreaDefinitionDto(
    string AreaKey,
    string DisplayName,
    int UnlockLevel,
    bool IsStartingArea,
    IReadOnlyList<string> NormalEnemyKeys,
    IReadOnlyList<string> DungeonKeys);
