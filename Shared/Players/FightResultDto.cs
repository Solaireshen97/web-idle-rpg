namespace Shared.Players;

public sealed record FightResultDto(
    bool IsVictory,
    int GoldReward,
    string EnemyName,
    int EnemyMaxHp,
    int EnemyAttack,
    string Summary,
    PlayerDto Player);
