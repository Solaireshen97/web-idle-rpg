namespace Shared.Players;

public sealed record UseItemResultDto(
    string ActionName,
    string ItemKey,
    int ConsumedAmount,
    ResourceDeltaDto ResourcesDelta,
    int RecoveredHp,
    PlayerDto Player);
