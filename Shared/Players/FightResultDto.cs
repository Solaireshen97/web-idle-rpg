namespace Shared.Players;

public sealed record FightResultDto(
    bool IsVictory,
    int GoldReward,
    int ExperienceReward,
    FightRewardResultDto Rewards,
    bool LeveledUp,
    string EnemyName,
    int EnemyMaxHp,
    int EnemyAttack,
    IReadOnlyList<PlayerActionResultDto> PlayerActions,
    IReadOnlyList<EnemyActionResultDto> EnemyActions,
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

public sealed record EnemyActionResultDto(
    string ActionName,
    int DamageDealt,
    int PlayerHpAfterAction);

public sealed record FightRewardResultDto(
    int Gold,
    int Experience,
    int Food,
    ResourceDeltaDto ResourcesDelta);
