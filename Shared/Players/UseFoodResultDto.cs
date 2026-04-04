namespace Shared.Players;

public sealed record UseFoodResultDto(
    string ActionName,
    string ItemKey,
    int ConsumedAmount,
    ResourceDeltaDto ResourcesDelta,
    HoldingDeltaDto HoldingDelta,
    int RecoveredHp,
    PlayerDto Player,
    string? ResourceKey = null);
