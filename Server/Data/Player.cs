namespace Server.Data;

public class Player
{
    private const int DefaultLevel = 1;
    private const int DefaultAttack = 3;
    private const int DefaultMaxHp = 30;
    private const int DefaultFood = 3;
    private const string DefaultPreferredEnemyKey = "random";
    private const bool DefaultPowerStrikeEnabled = true;
    private const int DefaultPowerStrikeCooldownRemaining = 0;

    public Player()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Level = DefaultLevel;
        Attack = DefaultAttack;
        MaxHp = DefaultMaxHp;
        CurrentHp = DefaultMaxHp;
        Food = DefaultFood;
        PreferredEnemyKey = DefaultPreferredEnemyKey;
        PowerStrikeEnabled = DefaultPowerStrikeEnabled;
        PowerStrikeCooldownRemaining = DefaultPowerStrikeCooldownRemaining;
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public int Gold { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public int Food { get; set; }
    public int Attack { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public string? CurrentEnemyName { get; set; }
    public int? CurrentEnemyMaxHp { get; set; }
    public int? CurrentEnemyCurrentHp { get; set; }
    public int? CurrentEnemyAttack { get; set; }
    public int? CurrentEnemyGoldReward { get; set; }
    public int? CurrentEnemyExperienceReward { get; set; }
    public string PreferredEnemyKey { get; set; }
    public bool PowerStrikeEnabled { get; set; }
    public int PowerStrikeCooldownRemaining { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
