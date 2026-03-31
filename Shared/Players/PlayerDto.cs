namespace Shared.Players;

public sealed record PlayerDto(
    int Id,
    string Name,
    int Gold,
    int Level,
    int Experience,
    int Attack,
    int MaxHp,
    int CurrentHp,
    string? CurrentEnemyName,
    int? CurrentEnemyMaxHp,
    int? CurrentEnemyCurrentHp,
    int? CurrentEnemyAttack,
    int? CurrentEnemyGoldReward,
    int? CurrentEnemyExperienceReward,
    DateTime CreatedAt,
    DateTime UpdatedAt);
