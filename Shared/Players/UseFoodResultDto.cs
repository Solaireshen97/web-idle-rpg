namespace Shared.Players;

public sealed record UseFoodResultDto(
    string ActionName,
    string ResourceKey,
    int ConsumedAmount,
    ResourceDeltaDto ResourcesDelta,
    HoldingDeltaDto HoldingDelta,
    int RecoveredHp,
    PlayerDto Player);
