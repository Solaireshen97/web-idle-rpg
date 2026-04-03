namespace Shared.Players;

public sealed record UseFoodResultDto(
    string ActionName,
    string ResourceKey,
    int ConsumedAmount,
    int RecoveredHp,
    PlayerDto Player);
