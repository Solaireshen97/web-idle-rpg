namespace Shared.Players;

public sealed record DungeonDefinitionDto(
    string DungeonKey,
    string DisplayName,
    string AreaKey,
    int UnlockLevel,
    IReadOnlyList<DungeonWaveDefinitionDto> Waves);
