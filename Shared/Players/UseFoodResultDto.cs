namespace Shared.Players;

public sealed record UseFoodResultDto(
    string ActionName,
    string ResourceKey,
    int ConsumedAmount,
    ResourceDeltaDto ResourcesDelta,
    int RecoveredHp,
    PlayerDto Player);
