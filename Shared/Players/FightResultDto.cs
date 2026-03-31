namespace Shared.Players;

public sealed record FightResultDto(
    bool IsVictory,
    int GoldReward,
    PlayerDto Player);
