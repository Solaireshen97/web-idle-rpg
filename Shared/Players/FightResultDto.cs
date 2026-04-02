namespace Shared.Players;

public sealed record FightResultDto(
    bool IsVictory,
    int GoldReward,
    int ExperienceReward,
    bool LeveledUp,
    string EnemyName,
    int EnemyMaxHp,
    int EnemyAttack,
    string PlayerSkillName,
    IReadOnlyList<PlayerActionResultDto> PlayerActions,
    int EnemyCurrentHp,
    int PlayerDamageDealt,
    int EnemyDamageDealt,
    bool EnemyDefeated,
    bool PlayerDefeated,
    string Summary,
    PlayerDto Player);

public sealed record PlayerActionResultDto(
    string ActionName,
    int DamageDealt,
    int EnemyHpAfterAction);
