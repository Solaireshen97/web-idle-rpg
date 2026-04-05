namespace Shared.Players;

public sealed record EncounterMetadataDto(
    bool IsActive,
    string EncounterType,
    string EncounterKey,
    string EncounterName,
    int WaveIndex,
    int TotalWaves);
