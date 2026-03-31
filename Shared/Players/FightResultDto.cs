namespace Shared.Players;

public sealed record FightResultDto(
    bool IsVictory,
    int GoldReward,
    int ExperienceReward,
    bool LeveledUp,
    string EnemyName,
    int EnemyMaxHp,
    int EnemyAttack,
    string Summary,
    PlayerDto Player);
