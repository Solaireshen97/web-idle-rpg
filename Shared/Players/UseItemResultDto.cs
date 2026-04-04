namespace Shared.Players;

public sealed record UseItemResultDto(
    string ActionName,
    string ItemKey,
    int ConsumedAmount,
    ResourceDeltaDto ResourcesDelta,
    HoldingDeltaDto HoldingDelta,
    int RecoveredHp,
    PlayerDto Player);
