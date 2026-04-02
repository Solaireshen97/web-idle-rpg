using System.Data;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Players;

const int GoldIncrementAmount = 10;
const int FoodHealAmount = 10;
const int BaseExpPerLevel = 10;
const int ExpPerLevelGrowth = 5;
const int LevelUpAttackBonus = 1;
const int LevelUpMaxHpBonus = 5;
const int DefeatSurvivalHp = 1;
const string PowerStrikeSkillName = "Power Strike";
const string BasicAttackSkillName = "Basic Attack";
const string PlayersTableName = "Players";
const string PreferredEnemyRandomKey = "random";
const string PreferredEnemyTrainingSlimeKey = "training-slime";
const string PreferredEnemyWolfKey = "wolf";
const string PreferredEnemyGoblinKey = "goblin";

var supportedPreferredEnemyKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    PreferredEnemyRandomKey,
    PreferredEnemyTrainingSlimeKey,
    PreferredEnemyWolfKey,
    PreferredEnemyGoblinKey
};

var enemyTemplateByKey = new Dictionary<string, EnemyTemplate>(StringComparer.OrdinalIgnoreCase)
{
    [PreferredEnemyTrainingSlimeKey] = new EnemyTemplate("Training Slime", 24, 2, 5, 5),
    [PreferredEnemyWolfKey] = new EnemyTemplate("Wolf", 36, 3, 9, 8),
    [PreferredEnemyGoblinKey] = new EnemyTemplate("Goblin", 52, 4, 12, 11),
};

var enemyTemplates = enemyTemplateByKey.Values.ToArray();

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing connection string 'DefaultConnection'.");

Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "App_Data"));

builder.Services.AddDbContext<GameDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GameDbContext>();
    dbContext.Database.EnsureCreated();
    EnsurePlayerSchema(dbContext);
}

app.MapGet("/api/ping", () => Results.Ok(new { message = "pong" }));

app.MapPost("/api/players", async (GameDbContext dbContext, CreatePlayerRequest request) =>
{
    var name = request.Name?.Trim();
    if (string.IsNullOrWhiteSpace(name))
    {
        return Results.BadRequest(new { message = "Player name is required." });
    }

    var player = new Player
    {
        Name = name
    };

    dbContext.Players.Add(player);
    await dbContext.SaveChangesAsync();

    var playerDto = ToPlayerDto(player);
    return Results.Created($"/api/players/{player.Id}", playerDto);
});

app.MapGet("/api/players/{id:int}", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    return player is null ? Results.NotFound() : Results.Ok(ToPlayerDto(player));
});

app.MapPost("/api/players/{id:int}/gold", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    player.Gold += GoldIncrementAmount;
    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    return Results.Ok(ToPlayerDto(player));
});

app.MapPost("/api/players/{id:int}/use-food", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    if (player.Food <= 0)
    {
        return Results.BadRequest(new { message = "Not enough food." });
    }

    player.Food -= 1;
    player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + FoodHealAmount);
    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    return Results.Ok(ToPlayerDto(player));
});

app.MapPost("/api/players/{id:int}/preferred-enemy", async (GameDbContext dbContext, int id, SetPreferredEnemyRequest request) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    if (!TryGetPreferredEnemyKey(request.EnemyKey, out var enemyKey))
    {
        return Results.BadRequest(new { message = "Invalid enemyKey. Supported: random, training-slime, wolf, goblin." });
    }

    player.PreferredEnemyKey = enemyKey;
    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    return Results.Ok(ToPlayerDto(player));
});

app.MapPost("/api/players/{id:int}/fight", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    if (!HasCurrentEnemy(player))
    {
        var preferredEnemyKey = NormalizePreferredEnemyKey(player.PreferredEnemyKey);
        var enemyTemplate = GetEnemyTemplateForNewFight(preferredEnemyKey, enemyTemplateByKey, enemyTemplates);
        AssignCurrentEnemy(player, enemyTemplate);
    }

    var enemy = GetCurrentEnemyState(player);

    var playerMaxHp = Math.Max(1, player.MaxHp);
    var playerCurrentHp = Math.Min(playerMaxHp, Math.Max(0, player.CurrentHp));
    var enemyCurrentHp = Math.Max(0, enemy.CurrentHp);
    var playerActions = new List<PlayerActionResultDto>();

    var powerStrikeResult = ExecutePlayerPowerStrikeSkill(player, enemyCurrentHp);
    playerActions.Add(ToPlayerActionResultDto(powerStrikeResult));
    enemyCurrentHp = powerStrikeResult.EnemyHpAfterAction;

    if (enemyCurrentHp > 0)
    {
        var basicAttackResult = ExecutePlayerBasicAttackSkill(player, enemyCurrentHp);
        playerActions.Add(ToPlayerActionResultDto(basicAttackResult));
        enemyCurrentHp = basicAttackResult.EnemyHpAfterAction;
    }

    var playerSkillResult = playerActions[^1];
    var playerDamageDealt = playerActions.Sum(action => action.DamageDealt);
    var enemyDefeated = enemyCurrentHp <= 0;

    var enemyDamageDealt = 0;
    if (!enemyDefeated)
    {
        enemyDamageDealt = Math.Min(enemy.Attack, playerCurrentHp);
        playerCurrentHp -= enemy.Attack;
    }

    var playerDefeated = playerCurrentHp <= 0;
    var goldReward = enemyDefeated ? enemy.GoldReward : 0;
    var expReward = enemyDefeated ? enemy.ExperienceReward : 0;
    var leveledUp = false;
    if (enemyDefeated)
    {
        player.Gold += goldReward;
        player.Experience += expReward;
        player.Food += 1;
        player.CurrentHp = Math.Max(0, playerCurrentHp);
        var levelUpHpRecovery = 0;

        while (true)
        {
            var requiredExpForNextLevel = GetRequiredExpForNextLevel(player.Level);
            if (player.Experience < requiredExpForNextLevel)
            {
                break;
            }

            player.Experience -= requiredExpForNextLevel;
            player.Level += 1;
            player.Attack += LevelUpAttackBonus;
            player.MaxHp += LevelUpMaxHpBonus;
            levelUpHpRecovery += LevelUpMaxHpBonus;
            leveledUp = true;
        }

        if (leveledUp)
        {
            player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + levelUpHpRecovery);
        }

        ClearCurrentEnemy(player);
    }
    else if (playerDefeated)
    {
        player.CurrentHp = DefeatSurvivalHp;
        ClearCurrentEnemy(player);
    }
    else
    {
        player.CurrentHp = playerCurrentHp;
        player.CurrentEnemyCurrentHp = enemyCurrentHp;
    }

    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    var summary = enemyDefeated
        ? $"{player.Name} defeated {enemy.Name} and earned {goldReward} gold, {expReward} EXP, 1 Food."
            + (leveledUp ? $" Level up! Now Lv{player.Level}." : "")
        : playerDefeated
            ? $"{player.Name} was defeated by {enemy.Name}. Enemy was reset. HP is now {player.CurrentHp}/{playerMaxHp}. Use Food before continuing."
            : $"{player.Name} used {string.Join(" -> ", playerActions.Select(action => action.ActionName))} and dealt {playerDamageDealt} total to {enemy.Name}. {enemy.Name} dealt {enemyDamageDealt}. Enemy HP: {enemyCurrentHp}/{enemy.MaxHp}.";

    return Results.Ok(new FightResultDto(
        enemyDefeated,
        goldReward,
        expReward,
        leveledUp,
        enemy.Name,
        enemy.MaxHp,
        enemy.Attack,
        playerSkillResult.ActionName,
        playerActions,
        Math.Max(0, enemyCurrentHp),
        playerDamageDealt,
        enemyDamageDealt,
        enemyDefeated,
        playerDefeated,
        summary,
        ToPlayerDto(player)));
});

app.Run();

static PlayerDto ToPlayerDto(Player player) =>
    new(
        player.Id,
        player.Name,
        player.Gold,
        player.Level,
        player.Experience,
        player.Food,
        player.Attack,
        player.MaxHp,
        player.CurrentHp,
        player.CurrentEnemyName,
        player.CurrentEnemyMaxHp,
        player.CurrentEnemyCurrentHp,
        player.CurrentEnemyAttack,
        player.CurrentEnemyGoldReward,
        player.CurrentEnemyExperienceReward,
        NormalizePreferredEnemyKey(player.PreferredEnemyKey),
        player.CreatedAt,
        player.UpdatedAt);

static bool HasCurrentEnemy(Player player) =>
    !string.IsNullOrWhiteSpace(player.CurrentEnemyName)
    && player.CurrentEnemyMaxHp.HasValue
    && player.CurrentEnemyCurrentHp.HasValue
    && player.CurrentEnemyAttack.HasValue
    && player.CurrentEnemyGoldReward.HasValue
    && player.CurrentEnemyExperienceReward.HasValue;

static CurrentEnemyState GetCurrentEnemyState(Player player)
{
    if (!HasCurrentEnemy(player))
    {
        throw new InvalidOperationException("Current enemy state is incomplete.");
    }

    return new CurrentEnemyState(
        player.CurrentEnemyName!,
        Math.Max(1, player.CurrentEnemyMaxHp!.Value),
        Math.Max(0, player.CurrentEnemyCurrentHp!.Value),
        Math.Max(1, player.CurrentEnemyAttack!.Value),
        Math.Max(0, player.CurrentEnemyGoldReward!.Value),
        Math.Max(0, player.CurrentEnemyExperienceReward!.Value));
}

static void AssignCurrentEnemy(Player player, EnemyTemplate enemy)
{
    player.CurrentEnemyName = enemy.Name;
    player.CurrentEnemyMaxHp = enemy.MaxHp;
    player.CurrentEnemyCurrentHp = enemy.MaxHp;
    player.CurrentEnemyAttack = enemy.Attack;
    player.CurrentEnemyGoldReward = enemy.GoldReward;
    player.CurrentEnemyExperienceReward = enemy.ExperienceReward;
}

static void ClearCurrentEnemy(Player player)
{
    player.CurrentEnemyName = null;
    player.CurrentEnemyMaxHp = null;
    player.CurrentEnemyCurrentHp = null;
    player.CurrentEnemyAttack = null;
    player.CurrentEnemyGoldReward = null;
    player.CurrentEnemyExperienceReward = null;
}

static int GetRequiredExpForNextLevel(int currentLevel) =>
    BaseExpPerLevel + (currentLevel - 1) * ExpPerLevelGrowth;

static void EnsurePlayerSchema(GameDbContext dbContext)
{
    var existingColumns = GetPlayerColumnNames(dbContext);
    AddPlayerColumnIfMissing(dbContext, existingColumns, "Level");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "Experience");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "Food");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEnemyName");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEnemyMaxHp");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEnemyCurrentHp");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEnemyAttack");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEnemyGoldReward");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEnemyExperienceReward");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "PreferredEnemyKey");
}

static HashSet<string> GetPlayerColumnNames(GameDbContext dbContext)
{
    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = $@"PRAGMA table_info(""{PlayersTableName}"")";

        using var reader = command.ExecuteReader();
        var columnNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            columnNames.Add(reader.GetString(1));
        }

        return columnNames;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static void AddPlayerColumnIfMissing(GameDbContext dbContext, HashSet<string> existingColumns, string columnName)
{
    if (existingColumns.Contains(columnName))
    {
        return;
    }

    var connection = dbContext.Database.GetDbConnection();
    var shouldClose = connection.State != ConnectionState.Open;
    if (shouldClose)
    {
        connection.Open();
    }

    try
    {
        using var command = connection.CreateCommand();
        command.CommandText = columnName switch
        {
            "Level" => @"ALTER TABLE ""Players"" ADD COLUMN ""Level"" INTEGER NOT NULL DEFAULT 1",
            "Experience" => @"ALTER TABLE ""Players"" ADD COLUMN ""Experience"" INTEGER NOT NULL DEFAULT 0",
            "Food" => @"ALTER TABLE ""Players"" ADD COLUMN ""Food"" INTEGER NOT NULL DEFAULT 3",
            "CurrentEnemyName" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEnemyName"" TEXT NULL",
            "CurrentEnemyMaxHp" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEnemyMaxHp"" INTEGER NULL",
            "CurrentEnemyCurrentHp" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEnemyCurrentHp"" INTEGER NULL",
            "CurrentEnemyAttack" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEnemyAttack"" INTEGER NULL",
            "CurrentEnemyGoldReward" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEnemyGoldReward"" INTEGER NULL",
            "CurrentEnemyExperienceReward" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEnemyExperienceReward"" INTEGER NULL",
            "PreferredEnemyKey" => @"ALTER TABLE ""Players"" ADD COLUMN ""PreferredEnemyKey"" TEXT NOT NULL DEFAULT 'random'",
            _ => throw new InvalidOperationException($"Unsupported Players column '{columnName}'.")
        };
        command.ExecuteNonQuery();
        existingColumns.Add(columnName);
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
}

static string NormalizePreferredEnemyKey(string? preferredEnemyKey)
{
    return TryGetPreferredEnemyKey(preferredEnemyKey, out var normalizedKey)
        ? normalizedKey
        : PreferredEnemyRandomKey;
}

static bool TryGetPreferredEnemyKey(string? preferredEnemyKey, out string normalizedKey)
{
    normalizedKey = preferredEnemyKey?.Trim().ToLowerInvariant() ?? string.Empty;
    switch (normalizedKey)
    {
        case PreferredEnemyRandomKey:
        case PreferredEnemyTrainingSlimeKey:
        case PreferredEnemyWolfKey:
        case PreferredEnemyGoblinKey:
            return true;
        default:
            normalizedKey = PreferredEnemyRandomKey;
            return false;
    }
}

static EnemyTemplate GetEnemyTemplateForNewFight(
    string preferredEnemyKey,
    Dictionary<string, EnemyTemplate> enemyTemplateByKey,
    EnemyTemplate[] enemyTemplates)
{
    if (preferredEnemyKey == PreferredEnemyRandomKey)
    {
        return enemyTemplates[Random.Shared.Next(enemyTemplates.Length)];
    }

    return enemyTemplateByKey.TryGetValue(preferredEnemyKey, out var enemyTemplate)
        ? enemyTemplate
        : enemyTemplates[Random.Shared.Next(enemyTemplates.Length)];
}

static PlayerSkillExecutionResult ExecutePlayerBasicAttackSkill(Player player, int enemyCurrentHp)
{
    var normalizedEnemyCurrentHp = Math.Max(0, enemyCurrentHp);
    var normalizedPlayerAttack = Math.Max(1, player.Attack);
    var damageDealt = Math.Min(normalizedPlayerAttack, normalizedEnemyCurrentHp);
    var enemyHpAfterAction = normalizedEnemyCurrentHp - damageDealt;

    return new PlayerSkillExecutionResult(
        BasicAttackSkillName,
        damageDealt,
        enemyHpAfterAction);
}

static PlayerSkillExecutionResult ExecutePlayerPowerStrikeSkill(Player player, int enemyCurrentHp)
{
    var normalizedEnemyCurrentHp = Math.Max(0, enemyCurrentHp);
    var normalizedPlayerAttack = Math.Max(1, player.Attack);
    var powerStrikeDamage = normalizedPlayerAttack + 1;
    var damageDealt = Math.Min(powerStrikeDamage, normalizedEnemyCurrentHp);
    var enemyHpAfterAction = normalizedEnemyCurrentHp - damageDealt;

    return new PlayerSkillExecutionResult(
        PowerStrikeSkillName,
        damageDealt,
        enemyHpAfterAction);
}

static PlayerActionResultDto ToPlayerActionResultDto(PlayerSkillExecutionResult skillResult) =>
    new(
        skillResult.SkillName,
        skillResult.DamageDealt,
        skillResult.EnemyHpAfterAction);

file sealed record EnemyTemplate(
    string Name,
    int MaxHp,
    int Attack,
    int GoldReward,
    int ExperienceReward);

file sealed record CurrentEnemyState(
    string Name,
    int MaxHp,
    int CurrentHp,
    int Attack,
    int GoldReward,
    int ExperienceReward);

file sealed record PlayerSkillExecutionResult(
    string SkillName,
    int DamageDealt,
    int EnemyHpAfterAction);
