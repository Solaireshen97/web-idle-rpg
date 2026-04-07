using System.Data;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Players;
using Shared.Shop;

const int GoldIncrementAmount = 10;
const int FoodHealAmount = 10;
const int PotionHealAmount = 20;
const int UseFoodConsumedAmount = 1;
const int BaseExpPerLevel = 10;
const int ExpPerLevelGrowth = 5;
const int LevelUpAttackBonus = 1;
const int LevelUpMaxHpBonus = 5;
const int DefeatSurvivalHp = 1;
const int FoodRewardPerEnemyDefeat = 1;
const int PowerStrikeBonusDamage = 1;
const int PowerStrikeCooldownTurns = 2;
const string PowerStrikeSkillName = "Power Strike";
const string BasicAttackSkillName = "Basic Attack";
const string EnemyJabActionName = "Enemy Jab";
const string EnemyAttackActionName = "Enemy Attack";
const string PlayersTableName = "Players";
const string PlayerItemHoldingsTableName = "PlayerItemHoldings";
const string FoodItemKey = "food";
const string PotionItemKey = "potion";
const string PreferredEnemyRandomKey = "random";
const string PreferredEnemyTrainingSlimeKey = "training-slime";
const string PreferredEnemyForestSpiderKey = "forest-spider";
const string PreferredEnemyWolfKey = "wolf";
const string PreferredEnemyGoblinKey = "goblin";
const string PreferredEnemyDefiasBanditKey = "defias-bandit";
const string PreferredEnemyHarvestGolemKey = "harvest-golem";
const string AreaElwynnForestKey = "elwynn-forest";
const string AreaWestfallKey = "westfall";
const string EncounterTypeNormal = "normal";
const int NormalEncounterSingleWaveIndex = 1;
const int NormalEncounterSingleWaveTotal = 1;

var enemyTemplateByKey = new Dictionary<string, EnemyTemplate>(StringComparer.OrdinalIgnoreCase)
{
    [PreferredEnemyTrainingSlimeKey] = new EnemyTemplate("Training Slime", 24, 2, 5, 5),
    [PreferredEnemyForestSpiderKey] = new EnemyTemplate("Forest Spider", 30, 3, 7, 7),
    [PreferredEnemyWolfKey] = new EnemyTemplate("Wolf", 38, 3, 9, 10),
    [PreferredEnemyGoblinKey] = new EnemyTemplate("Goblin", 58, 5, 14, 13),
    [PreferredEnemyDefiasBanditKey] = new EnemyTemplate("Defias Bandit", 64, 5, 16, 15),
    [PreferredEnemyHarvestGolemKey] = new EnemyTemplate("Harvest Golem", 72, 6, 18, 18),
};

var enemyTemplates = enemyTemplateByKey.Values.ToArray();
var areaDefinitions = new[]
{
    new AreaDefinitionDto(
        AreaKey: AreaElwynnForestKey,
        DisplayName: "Elwynn Forest",
        UnlockLevel: 1,
        IsStartingArea: true,
        NormalEnemyKeys: new[] { PreferredEnemyTrainingSlimeKey, PreferredEnemyForestSpiderKey, PreferredEnemyWolfKey },
        DungeonKeys: new[] { "elwynn-forest-training-grounds" }),
    new AreaDefinitionDto(
        AreaKey: AreaWestfallKey,
        DisplayName: "Westfall",
        UnlockLevel: 10,
        IsStartingArea: false,
        NormalEnemyKeys: new[] { PreferredEnemyGoblinKey, PreferredEnemyDefiasBanditKey, PreferredEnemyHarvestGolemKey },
        DungeonKeys: new[] { "westfall-abandoned-mine" })
};
var areaByKey = areaDefinitions.ToDictionary(area => area.AreaKey, StringComparer.OrdinalIgnoreCase);
var startingArea = areaDefinitions.First(area => area.IsStartingArea);
var shopItems = new[]
{
    new ShopItemDefinitionDto(
        ItemKey: "food",
        DisplayName: "Food",
        GoldPrice: 5,
        Effect: new ShopItemEffectDto(FoodDelta: 1, HpRecover: FoodHealAmount),
        ConsumableUse: new ConsumableUseMetadataDto(
            ConsumedAmount: UseFoodConsumedAmount,
            HpRecover: FoodHealAmount)),
    new ShopItemDefinitionDto(
        ItemKey: "food-pack",
        DisplayName: "Food Pack",
        GoldPrice: 12,
        Effect: new ShopItemEffectDto(FoodDelta: 3, HpRecover: FoodHealAmount),
        ConsumableUse: null),
    new ShopItemDefinitionDto(
        ItemKey: "food-crate",
        DisplayName: "Food Crate",
        GoldPrice: 18,
        Effect: new ShopItemEffectDto(FoodDelta: 5, HpRecover: FoodHealAmount),
        ConsumableUse: null),
    new ShopItemDefinitionDto(
        ItemKey: PotionItemKey,
        DisplayName: "Potion",
        GoldPrice: 10,
        Effect: new ShopItemEffectDto(FoodDelta: 0, HpRecover: PotionHealAmount),
        ConsumableUse: new ConsumableUseMetadataDto(
            ConsumedAmount: 1,
            HpRecover: PotionHealAmount))
};
var shopItemByKey = shopItems.ToDictionary(item => item.ItemKey, StringComparer.OrdinalIgnoreCase);

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
    EnsurePlayerItemHoldingsSchema(dbContext);
}

app.MapGet("/api/ping", () => Results.Ok(new { message = "pong" }));

app.MapGet("/api/shop/items", () => Results.Ok(shopItems));
app.MapGet("/api/shop/items/food", () => Results.Ok(shopItemByKey["food"]));
app.MapGet("/api/areas", () => Results.Ok(areaDefinitions));

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
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    return Results.Created($"/api/players/{player.Id}", playerDto);
});

app.MapGet("/api/players/{id:int}", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    return Results.Ok(playerDto);
});

app.MapGet("/api/players/{id:int}/holdings", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    var holdings = await BuildPlayerItemHoldingDtosAsync(dbContext, player);
    return Results.Ok(holdings);
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
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    return Results.Ok(playerDto);
});

app.MapPost("/api/players/{id:int}/use-food", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    var execution = await TryUseConsumableItemAsync(dbContext, player, FoodItemKey, shopItemByKey);
    if (!execution.Success)
    {
        return BuildConsumableUseErrorResult(execution.StatusCode, execution.Message);
    }

    return Results.Ok(BuildUseFoodResultFromUseItemResult(execution.UseItemResult!));
});

app.MapPost("/api/players/{id:int}/use-item/{itemKey}", async (GameDbContext dbContext, int id, string itemKey) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    var execution = await TryUseConsumableItemAsync(dbContext, player, itemKey, shopItemByKey);
    if (!execution.Success)
    {
        return BuildConsumableUseErrorResult(execution.StatusCode, execution.Message);
    }

    return Results.Ok(execution.UseItemResult);
});

app.MapPost("/api/players/{id:int}/buy-food", async (GameDbContext dbContext, int id) =>
{
    return await BuyShopItemAsync(dbContext, id, "food", shopItemByKey);
});

app.MapPost("/api/players/{id:int}/buy-item/{itemKey}", async (GameDbContext dbContext, int id, string itemKey) =>
{
    return await BuyShopItemAsync(dbContext, id, itemKey, shopItemByKey);
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
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    return Results.Ok(playerDto);
});

app.MapPost("/api/players/{id:int}/current-area", async (GameDbContext dbContext, int id, SetCurrentAreaRequest request) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    if (!TryGetAreaDefinition(areaByKey, request.AreaKey, out var area))
    {
        return Results.BadRequest(new { message = "Invalid areaKey." });
    }

    if (player.Level < area.UnlockLevel)
    {
        return Results.BadRequest(new { message = $"Area '{area.DisplayName}' unlocks at level {area.UnlockLevel}." });
    }

    player.CurrentAreaKey = area.AreaKey;
    ClearCurrentEncounter(player);
    ClearCurrentEnemy(player);
    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    return Results.Ok(playerDto);
});

app.MapPost("/api/players/{id:int}/power-strike", async (GameDbContext dbContext, int id, SetPowerStrikeRequest request) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    player.PowerStrikeEnabled = request.Enabled;
    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    return Results.Ok(playerDto);
});

app.MapPost("/api/players/{id:int}/fight", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    EnsurePlayerCurrentArea(player, areaByKey, startingArea);
    EnsureNormalEncounterAndEnemyForFight(player, areaByKey, enemyTemplateByKey, enemyTemplates, startingArea);

    var enemy = GetCurrentEnemyState(player);

    var playerMaxHp = Math.Max(1, player.MaxHp);
    var playerCurrentHp = Math.Min(playerMaxHp, Math.Max(0, player.CurrentHp));
    var enemyCurrentHp = Math.Max(0, enemy.CurrentHp);
    var powerStrikeCooldownAtTurnStart = Math.Max(0, player.PowerStrikeCooldownRemaining);
    var playerActionSequence = BuildPlayerTurnActionSequence(player, powerStrikeCooldownAtTurnStart);
    var playerTurnResult = ExecutePlayerTurnActionSequence(player, playerActionSequence, enemyCurrentHp);
    var playerActions = playerTurnResult.Actions;
    enemyCurrentHp = playerTurnResult.EnemyHpAfterTurn;
    var shouldUsePowerStrike = playerActionSequence.Contains(PlayerTurnActionType.PowerStrike);

    player.PowerStrikeCooldownRemaining = shouldUsePowerStrike
        ? Math.Max(0, PowerStrikeCooldownTurns - 1)
        : Math.Max(0, powerStrikeCooldownAtTurnStart - 1);

    var playerDamageDealt = playerTurnResult.TotalDamageDealt;
    var enemyDefeated = enemyCurrentHp <= 0;

    var enemyActions = new List<EnemyActionResultDto>();
    var enemyDamageDealt = 0;
    if (!enemyDefeated)
    {
        var enemyActionSequence = BuildEnemyTurnActionSequence(enemy, enemyCurrentHp);
        var enemyTurnResult = ExecuteEnemyTurnActionSequence(enemy, enemyActionSequence, playerCurrentHp);
        enemyActions = enemyTurnResult.Actions;
        playerCurrentHp = enemyTurnResult.PlayerHpAfterTurn;
        enemyDamageDealt = enemyTurnResult.TotalDamageDealt;
    }

    var playerDefeated = playerCurrentHp <= 0;
    var settlementResult = await ApplyFightRoundSettlementAsync(
        dbContext,
        player,
        enemy,
        enemyCurrentHp,
        playerCurrentHp,
        enemyDefeated,
        playerDefeated);

    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    var summary = BuildFightSummary(
        player,
        enemy,
        settlementResult,
        playerActions,
        playerDamageDealt,
        enemyDamageDealt,
        enemyCurrentHp);
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);

    return Results.Ok(new FightResultDto(
        enemyDefeated,
        settlementResult.GoldReward,
        settlementResult.ExpReward,
        BuildFightRewardResultDto(settlementResult),
        settlementResult.LeveledUp,
        enemy.Name,
        enemy.MaxHp,
        enemy.Attack,
        playerActions,
        enemyActions,
        Math.Max(0, enemyCurrentHp),
        playerDamageDealt,
        enemyDamageDealt,
        enemyDefeated,
        playerDefeated,
        summary,
        playerDto));
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
        ResolveCurrentAreaKey(player),
        ResolveAreaDisplayNameByKey(ResolveCurrentAreaKey(player)),
        BuildCurrentEncounterMetadata(player),
        NormalizePreferredEnemyKey(player.PreferredEnemyKey),
        player.PowerStrikeEnabled,
        Math.Max(0, player.PowerStrikeCooldownRemaining),
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

static void AssignCurrentEncounter(
    Player player,
    string encounterType,
    string encounterKey,
    string encounterName,
    int waveIndex,
    int totalWaves)
{
    player.CurrentEncounterType = encounterType;
    player.CurrentEncounterKey = encounterKey;
    player.CurrentEncounterName = encounterName;
    player.CurrentEncounterWaveIndex = waveIndex;
    player.CurrentEncounterTotalWaves = totalWaves;
}

static void ClearCurrentEncounter(Player player)
{
    player.CurrentEncounterType = null;
    player.CurrentEncounterKey = null;
    player.CurrentEncounterName = null;
    player.CurrentEncounterWaveIndex = null;
    player.CurrentEncounterTotalWaves = null;
}

static bool HasCurrentEncounter(Player player) =>
    !string.IsNullOrWhiteSpace(player.CurrentEncounterType)
    && !string.IsNullOrWhiteSpace(player.CurrentEncounterKey)
    && !string.IsNullOrWhiteSpace(player.CurrentEncounterName)
    && player.CurrentEncounterWaveIndex.HasValue
    && player.CurrentEncounterTotalWaves.HasValue;

static string ResolveCurrentAreaKey(Player player)
{
    if (string.IsNullOrWhiteSpace(player.CurrentAreaKey))
    {
        return AreaElwynnForestKey;
    }

    return player.CurrentAreaKey.Trim().ToLowerInvariant();
}

static string ResolveAreaDisplayNameByKey(string areaKey) =>
    areaKey switch
    {
        AreaWestfallKey => "Westfall",
        _ => "Elwynn Forest"
    };

static EncounterMetadataDto? BuildCurrentEncounterMetadata(Player player)
{
    if (!HasCurrentEncounter(player))
    {
        return null;
    }

    return new EncounterMetadataDto(
        IsActive: true,
        EncounterType: player.CurrentEncounterType!,
        EncounterKey: player.CurrentEncounterKey!,
        EncounterName: player.CurrentEncounterName!,
        WaveIndex: Math.Max(1, player.CurrentEncounterWaveIndex!.Value),
        TotalWaves: Math.Max(1, player.CurrentEncounterTotalWaves!.Value));
}

static int GetRequiredExpForNextLevel(int currentLevel) =>
    BaseExpPerLevel + (currentLevel - 1) * ExpPerLevelGrowth;

static async Task<FightSettlementResult> ApplyFightRoundSettlementAsync(
    GameDbContext dbContext,
    Player player,
    CurrentEnemyState enemy,
    int enemyCurrentHp,
    int playerCurrentHp,
    bool enemyDefeated,
    bool playerDefeated)
{
    if (enemyDefeated)
    {
        var enemyDefeatSettlementResult = await ApplyEnemyDefeatSettlementAsync(dbContext, player, enemy, playerCurrentHp);
        ClearCurrentEncounter(player);
        ClearCurrentEnemy(player);
        return new FightSettlementResult(
            FightSettlementBranch.EnemyDefeated,
            enemyDefeatSettlementResult.GoldReward,
            enemyDefeatSettlementResult.ExpReward,
            enemyDefeatSettlementResult.FoodReward,
            enemyDefeatSettlementResult.LeveledUp);
    }

    if (playerDefeated)
    {
        player.CurrentHp = DefeatSurvivalHp;
        ClearCurrentEncounter(player);
        ClearCurrentEnemy(player);
        return new FightSettlementResult(
            FightSettlementBranch.PlayerDefeated,
            0,
            0,
            0,
            false);
    }

    player.CurrentHp = playerCurrentHp;
    player.CurrentEnemyCurrentHp = enemyCurrentHp;
    return new FightSettlementResult(
        FightSettlementBranch.Ongoing,
        0,
        0,
        0,
        false);
}

static async Task<EnemyDefeatSettlementResult> ApplyEnemyDefeatSettlementAsync(
    GameDbContext dbContext,
    Player player,
    CurrentEnemyState enemy,
    int playerCurrentHp)
{
    var goldReward = enemy.GoldReward;
    var expReward = enemy.ExperienceReward;
    player.Gold += goldReward;
    player.Experience += expReward;
    var foodHolding = await GetOrCreateAndPersistFoodHoldingAsync(dbContext, player);
    UpdateItemHoldingQuantityInMemory(foodHolding, FoodRewardPerEnemyDefeat);
    SyncFoodProjection(player, foodHolding);
    player.CurrentHp = Math.Max(0, playerCurrentHp);

    var levelUpHpRecovery = 0;
    var leveledUp = false;
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

    return new EnemyDefeatSettlementResult(
        goldReward,
        expReward,
        FoodRewardPerEnemyDefeat,
        leveledUp);
}

static string BuildFightSummary(
    Player player,
    CurrentEnemyState enemy,
    FightSettlementResult settlementResult,
    IReadOnlyList<PlayerActionResultDto> playerActions,
    int playerDamageDealt,
    int enemyDamageDealt,
    int enemyCurrentHp) =>
    settlementResult.Branch switch
    {
        FightSettlementBranch.EnemyDefeated =>
            $"{player.Name} defeated {enemy.Name} and earned {settlementResult.GoldReward} gold, {settlementResult.ExpReward} EXP, {settlementResult.FoodReward} Food."
            + (settlementResult.LeveledUp ? $" Level up! Now Lv{player.Level}." : ""),
        FightSettlementBranch.PlayerDefeated =>
            $"{player.Name} was defeated by {enemy.Name}. Enemy was reset. HP is now {player.CurrentHp}/{player.MaxHp}. Use Food before continuing.",
        _ =>
            $"{player.Name} used {string.Join(" -> ", playerActions.Select(action => action.ActionName))} and dealt {playerDamageDealt} total to {enemy.Name}. {enemy.Name} dealt {enemyDamageDealt}. Enemy HP: {enemyCurrentHp}/{enemy.MaxHp}."
    };

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
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentAreaKey");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEncounterType");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEncounterKey");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEncounterName");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEncounterWaveIndex");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "CurrentEncounterTotalWaves");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "PreferredEnemyKey");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "PowerStrikeEnabled");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "PowerStrikeCooldownRemaining");
}

static void EnsurePlayerItemHoldingsSchema(GameDbContext dbContext)
{
    var existingTables = GetSqliteTableNames(dbContext);
    if (!existingTables.Contains(PlayerItemHoldingsTableName))
    {
        dbContext.Database.ExecuteSqlRaw($@"
CREATE TABLE ""{PlayerItemHoldingsTableName}"" (
    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_{PlayerItemHoldingsTableName}"" PRIMARY KEY AUTOINCREMENT,
    ""PlayerId"" INTEGER NOT NULL,
    ""ItemKey"" TEXT NOT NULL,
    ""Quantity"" INTEGER NOT NULL
);");
    }

    dbContext.Database.ExecuteSqlRaw($@"
CREATE UNIQUE INDEX IF NOT EXISTS ""IX_{PlayerItemHoldingsTableName}_PlayerId_ItemKey""
ON ""{PlayerItemHoldingsTableName}"" (""PlayerId"", ""ItemKey"");");
}

static HashSet<string> GetSqliteTableNames(GameDbContext dbContext)
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
        command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table'";
        using var reader = command.ExecuteReader();
        var tableNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read())
        {
            tableNames.Add(reader.GetString(0));
        }

        return tableNames;
    }
    finally
    {
        if (shouldClose)
        {
            connection.Close();
        }
    }
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
            "CurrentAreaKey" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentAreaKey"" TEXT NOT NULL DEFAULT 'elwynn-forest'",
            "CurrentEncounterType" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEncounterType"" TEXT NULL",
            "CurrentEncounterKey" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEncounterKey"" TEXT NULL",
            "CurrentEncounterName" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEncounterName"" TEXT NULL",
            "CurrentEncounterWaveIndex" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEncounterWaveIndex"" INTEGER NULL",
            "CurrentEncounterTotalWaves" => @"ALTER TABLE ""Players"" ADD COLUMN ""CurrentEncounterTotalWaves"" INTEGER NULL",
            "PreferredEnemyKey" => @"ALTER TABLE ""Players"" ADD COLUMN ""PreferredEnemyKey"" TEXT NOT NULL DEFAULT 'random'",
            "PowerStrikeEnabled" => @"ALTER TABLE ""Players"" ADD COLUMN ""PowerStrikeEnabled"" INTEGER NOT NULL DEFAULT 1",
            "PowerStrikeCooldownRemaining" => @"ALTER TABLE ""Players"" ADD COLUMN ""PowerStrikeCooldownRemaining"" INTEGER NOT NULL DEFAULT 0",
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

static bool TryGetAreaDefinition(
    IReadOnlyDictionary<string, AreaDefinitionDto> areaByKey,
    string? areaKey,
    out AreaDefinitionDto area)
{
    if (string.IsNullOrWhiteSpace(areaKey))
    {
        area = default!;
        return false;
    }

    return areaByKey.TryGetValue(areaKey.Trim(), out area!);
}

static void EnsurePlayerCurrentArea(
    Player player,
    IReadOnlyDictionary<string, AreaDefinitionDto> areaByKey,
    AreaDefinitionDto startingArea)
{
    if (TryGetAreaDefinition(areaByKey, player.CurrentAreaKey, out _))
    {
        player.CurrentAreaKey = ResolveCurrentAreaKey(player);
        return;
    }

    player.CurrentAreaKey = startingArea.AreaKey;
}

static void EnsureNormalEncounterAndEnemyForFight(
    Player player,
    IReadOnlyDictionary<string, AreaDefinitionDto> areaByKey,
    IReadOnlyDictionary<string, EnemyTemplate> enemyTemplateByKey,
    EnemyTemplate[] enemyTemplates,
    AreaDefinitionDto startingArea)
{
    EnsurePlayerCurrentArea(player, areaByKey, startingArea);
    if (HasCurrentEncounter(player) && HasCurrentEnemy(player))
    {
        return;
    }

    if (!TryGetAreaDefinition(areaByKey, player.CurrentAreaKey, out var currentArea))
    {
        currentArea = startingArea;
        player.CurrentAreaKey = startingArea.AreaKey;
    }

    var preferredEnemyKey = NormalizePreferredEnemyKey(player.PreferredEnemyKey);
    var enemyTemplate = GetEnemyTemplateForNewFightInArea(preferredEnemyKey, currentArea, enemyTemplateByKey, enemyTemplates);
    var encounterKey = $"normal:{currentArea.AreaKey}:{enemyTemplate.Name.ToLowerInvariant().Replace(" ", "-")}";
    AssignCurrentEncounter(
        player,
        EncounterTypeNormal,
        encounterKey,
        $"{currentArea.DisplayName} - Normal Encounter",
        NormalEncounterSingleWaveIndex,
        NormalEncounterSingleWaveTotal);
    AssignCurrentEnemy(player, enemyTemplate);
}

static async Task<IResult> BuyShopItemAsync(
    GameDbContext dbContext,
    int playerId,
    string itemKey,
    IReadOnlyDictionary<string, ShopItemDefinitionDto> shopItemByKey)
{
    var player = await dbContext.Players.FindAsync(playerId);
    if (player is null)
    {
        return Results.NotFound(new { message = "Player not found." });
    }

    if (!TryGetShopItemDefinition(shopItemByKey, itemKey, out var item))
    {
        return Results.NotFound(new { message = "Shop item not found." });
    }

    if (!CanAffordShopItem(player, item, out var insufficientGoldMessage))
    {
        return Results.BadRequest(new { message = insufficientGoldMessage });
    }

    ApplyShopPurchase(player, item);
    await ApplyShopItemHoldingEffectAsync(dbContext, player, item);

    await dbContext.SaveChangesAsync();
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    return Results.Ok(new ShopPurchaseResultDto(
        item.ItemKey,
        item.DisplayName,
        item.GoldPrice,
        BuildShopPurchaseResourcesDelta(item),
        BuildShopPurchaseHoldingDelta(item),
        item.Effect,
        playerDto));
}

static bool TryGetShopItemDefinition(
    IReadOnlyDictionary<string, ShopItemDefinitionDto> shopItemByKey,
    string itemKey,
    out ShopItemDefinitionDto item)
{
    if (string.IsNullOrWhiteSpace(itemKey))
    {
        item = default!;
        return false;
    }

    return shopItemByKey.TryGetValue(itemKey, out item!);
}

static bool TryGetConsumableUseMetadata(
    ShopItemDefinitionDto item,
    out ConsumableUseMetadataDto consumableUse)
{
    if (item.ConsumableUse is null)
    {
        consumableUse = default!;
        return false;
    }

    consumableUse = item.ConsumableUse;

    if (consumableUse.ConsumedAmount <= 0)
    {
        return false;
    }

    if (consumableUse.HpRecover < 0)
    {
        return false;
    }

    return true;
}

static async Task<ConsumableUseExecutionResult> TryUseConsumableItemAsync(
    GameDbContext dbContext,
    Player player,
    string itemKey,
    IReadOnlyDictionary<string, ShopItemDefinitionDto> shopItemByKey)
{
    if (!TryGetShopItemDefinition(shopItemByKey, itemKey, out var item))
    {
        return ConsumableUseExecutionResult.NotFound("Consumable item not found.");
    }

    if (!TryGetConsumableUseMetadata(item, out var consumableUse))
    {
        return ConsumableUseExecutionResult.BadRequest("Item is not a supported consumable action.");
    }

    var holding = await GetOrCreateAndPersistHoldingAsync(dbContext, player, item.ItemKey);
    if (holding.Quantity <= 0)
    {
        return ConsumableUseExecutionResult.BadRequest($"Not enough {item.ItemKey}.");
    }

    var consumedAmount = consumableUse.ConsumedAmount;
    var hpBeforeUseItem = player.CurrentHp;
    UpdateItemHoldingQuantityInMemory(holding, -consumedAmount);
    await SyncFoodProjectionFromHoldingAsync(dbContext, player);
    player.CurrentHp = Math.Min(player.MaxHp, player.CurrentHp + consumableUse.HpRecover);
    var recoveredHp = Math.Max(0, player.CurrentHp - hpBeforeUseItem);
    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();
    var playerDto = await BuildPlayerDtoWithFoodProjectionAsync(dbContext, player);
    var result = new UseItemResultDto(
        ActionName: $"Use {item.DisplayName}",
        ItemKey: item.ItemKey,
        ConsumedAmount: consumedAmount,
        ResourcesDelta: BuildConsumableUseResourcesDelta(item.ItemKey, consumedAmount),
        HoldingDelta: BuildConsumableUseHoldingDelta(item.ItemKey, consumedAmount),
        RecoveredHp: recoveredHp,
        Player: playerDto);
    return ConsumableUseExecutionResult.Succeed(result);
}

static IResult BuildConsumableUseErrorResult(int statusCode, string message)
{
    if (statusCode == StatusCodes.Status404NotFound)
    {
        return Results.NotFound(new { message });
    }

    if (statusCode == StatusCodes.Status400BadRequest)
    {
        return Results.BadRequest(new { message });
    }

    return Results.Json(new { message }, statusCode: statusCode);
}

static UseFoodResultDto BuildUseFoodResultFromUseItemResult(UseItemResultDto useItemResult) =>
    new(
        ActionName: useItemResult.ActionName,
        ItemKey: useItemResult.ItemKey,
        ConsumedAmount: useItemResult.ConsumedAmount,
        ResourcesDelta: useItemResult.ResourcesDelta,
        HoldingDelta: useItemResult.HoldingDelta,
        RecoveredHp: useItemResult.RecoveredHp,
        Player: useItemResult.Player,
        ResourceKey: useItemResult.ItemKey);

static bool CanAffordShopItem(Player player, ShopItemDefinitionDto item, out string message)
{
    if (player.Gold >= item.GoldPrice)
    {
        message = string.Empty;
        return true;
    }

    message = $"Not enough gold. Need {item.GoldPrice} gold to buy {item.DisplayName}.";
    return false;
}

static void ApplyShopPurchase(Player player, ShopItemDefinitionDto item)
{
    player.Gold -= item.GoldPrice;
    player.UpdatedAt = DateTime.UtcNow;
}

static async Task ApplyShopItemHoldingEffectAsync(
    GameDbContext dbContext,
    Player player,
    ShopItemDefinitionDto item)
{
    var holding = await GetOrCreateAndPersistHoldingAsync(dbContext, player, item.ItemKey);
    if (item.ItemKey.Equals(FoodItemKey, StringComparison.OrdinalIgnoreCase))
    {
        UpdateItemHoldingQuantityInMemory(holding, item.Effect.FoodDelta);
        await SyncFoodProjectionFromHoldingAsync(dbContext, player);
        return;
    }

    UpdateItemHoldingQuantityInMemory(holding, 1);
}

static bool TryGetPreferredEnemyKey(string? preferredEnemyKey, out string normalizedKey)
{
    normalizedKey = preferredEnemyKey?.Trim().ToLowerInvariant() ?? string.Empty;
    switch (normalizedKey)
    {
        case PreferredEnemyRandomKey:
        case PreferredEnemyTrainingSlimeKey:
        case PreferredEnemyForestSpiderKey:
        case PreferredEnemyWolfKey:
        case PreferredEnemyGoblinKey:
        case PreferredEnemyDefiasBanditKey:
        case PreferredEnemyHarvestGolemKey:
            return true;
        default:
            normalizedKey = PreferredEnemyRandomKey;
            return false;
    }
}

static EnemyTemplate GetEnemyTemplateForNewFight(
    string preferredEnemyKey,
    IReadOnlyDictionary<string, EnemyTemplate> enemyTemplateByKey,
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

static EnemyTemplate GetEnemyTemplateForNewFightInArea(
    string preferredEnemyKey,
    AreaDefinitionDto area,
    IReadOnlyDictionary<string, EnemyTemplate> enemyTemplateByKey,
    EnemyTemplate[] fallbackEnemyTemplates)
{
    var areaEnemyTemplates = area.NormalEnemyKeys
        .Select(enemyKey =>
        {
            var normalizedEnemyKey = NormalizePreferredEnemyKey(enemyKey);
            return enemyTemplateByKey.TryGetValue(normalizedEnemyKey, out var enemyTemplate)
                ? enemyTemplate
                : null;
        })
        .Where(template => template is not null)
        .Cast<EnemyTemplate>()
        .ToArray();

    if (areaEnemyTemplates.Length <= 0)
    {
        return GetEnemyTemplateForNewFight(preferredEnemyKey, enemyTemplateByKey, fallbackEnemyTemplates);
    }

    if (preferredEnemyKey != PreferredEnemyRandomKey
        && area.NormalEnemyKeys.Any(enemyKey => NormalizePreferredEnemyKey(enemyKey) == preferredEnemyKey)
        && enemyTemplateByKey.TryGetValue(preferredEnemyKey, out var preferredEnemyTemplate))
    {
        return preferredEnemyTemplate;
    }

    return areaEnemyTemplates[Random.Shared.Next(areaEnemyTemplates.Length)];
}

static List<PlayerTurnActionType> BuildPlayerTurnActionSequence(Player player, int powerStrikeCooldownAtTurnStart)
{
    var actionSequence = new List<PlayerTurnActionType>();
    var shouldUsePowerStrike = player.PowerStrikeEnabled && powerStrikeCooldownAtTurnStart <= 0;
    if (shouldUsePowerStrike)
    {
        actionSequence.Add(PlayerTurnActionType.PowerStrike);
    }

    actionSequence.Add(PlayerTurnActionType.BasicAttack);
    return actionSequence;
}

static PlayerTurnExecutionResult ExecutePlayerTurnActionSequence(
    Player player,
    IReadOnlyList<PlayerTurnActionType> actionSequence,
    int enemyCurrentHp)
{
    var normalizedEnemyCurrentHp = Math.Max(0, enemyCurrentHp);
    var actions = new List<PlayerActionResultDto>();
    foreach (var actionType in actionSequence)
    {
        if (normalizedEnemyCurrentHp <= 0)
        {
            break;
        }

        var actionExecutionResult = actionType switch
        {
            PlayerTurnActionType.PowerStrike => ExecutePlayerPowerStrikeSkill(player, normalizedEnemyCurrentHp),
            PlayerTurnActionType.BasicAttack => ExecutePlayerBasicAttackSkill(player, normalizedEnemyCurrentHp),
            _ => throw new InvalidOperationException($"Unsupported player turn action type '{actionType}'.")
        };

        actions.Add(ToPlayerActionResultDto(actionExecutionResult));
        normalizedEnemyCurrentHp = actionExecutionResult.EnemyHpAfterAction;
    }

    return new PlayerTurnExecutionResult(
        actions,
        normalizedEnemyCurrentHp,
        actions.Sum(action => action.DamageDealt));
}

static List<EnemyTurnActionType> BuildEnemyTurnActionSequence(CurrentEnemyState enemy, int enemyCurrentHp)
{
    var actionSequence = new List<EnemyTurnActionType>();
    if (ShouldTriggerEnemyExtraAction(enemy, enemyCurrentHp))
    {
        actionSequence.Add(EnemyTurnActionType.EnemyJab);
    }

    actionSequence.Add(EnemyTurnActionType.EnemyAttack);
    return actionSequence;
}

static EnemyTurnExecutionResult ExecuteEnemyTurnActionSequence(
    CurrentEnemyState enemy,
    IReadOnlyList<EnemyTurnActionType> actionSequence,
    int playerCurrentHp)
{
    var normalizedPlayerCurrentHp = Math.Max(0, playerCurrentHp);
    var actions = new List<EnemyActionResultDto>();
    foreach (var actionType in actionSequence)
    {
        if (normalizedPlayerCurrentHp <= 0)
        {
            break;
        }

        var actionExecutionResult = actionType switch
        {
            EnemyTurnActionType.EnemyJab => ExecuteEnemyExtraAction(enemy, normalizedPlayerCurrentHp),
            EnemyTurnActionType.EnemyAttack => ExecuteEnemyAttackAction(enemy, normalizedPlayerCurrentHp),
            _ => throw new InvalidOperationException($"Unsupported enemy turn action type '{actionType}'.")
        };

        actions.Add(ToEnemyActionResultDto(actionExecutionResult));
        normalizedPlayerCurrentHp = actionExecutionResult.PlayerHpAfterAction;
    }

    return new EnemyTurnExecutionResult(
        actions,
        normalizedPlayerCurrentHp,
        actions.Sum(action => action.DamageDealt));
}

static PlayerSkillExecutionResult ExecutePlayerBasicAttackSkill(Player player, int enemyCurrentHp)
{
    var damageSettlement = ResolveDamageSettlement(
        baseAttackValue: player.Attack,
        flatDamageModifier: 0,
        minimumDamage: 0,
        targetCurrentHp: enemyCurrentHp);

    return new PlayerSkillExecutionResult(
        BasicAttackSkillName,
        damageSettlement.DamageDealt,
        damageSettlement.TargetHpAfterDamage);
}

static PlayerSkillExecutionResult ExecutePlayerPowerStrikeSkill(Player player, int enemyCurrentHp)
{
    var damageSettlement = ResolveDamageSettlement(
        baseAttackValue: player.Attack,
        flatDamageModifier: PowerStrikeBonusDamage,
        minimumDamage: 0,
        targetCurrentHp: enemyCurrentHp);

    return new PlayerSkillExecutionResult(
        PowerStrikeSkillName,
        damageSettlement.DamageDealt,
        damageSettlement.TargetHpAfterDamage);
}

static EnemyActionExecutionResult ExecuteEnemyAttackAction(CurrentEnemyState enemy, int playerCurrentHp)
{
    var damageSettlement = ResolveDamageSettlement(
        baseAttackValue: enemy.Attack,
        flatDamageModifier: 0,
        minimumDamage: 0,
        targetCurrentHp: playerCurrentHp);

    return new EnemyActionExecutionResult(
        EnemyAttackActionName,
        damageSettlement.DamageDealt,
        damageSettlement.TargetHpAfterDamage);
}

static EnemyActionExecutionResult ExecuteEnemyExtraAction(CurrentEnemyState enemy, int playerCurrentHp)
{
    var damageSettlement = ResolveDamageSettlement(
        baseAttackValue: enemy.Attack,
        flatDamageModifier: -1,
        minimumDamage: 1,
        targetCurrentHp: playerCurrentHp);

    return new EnemyActionExecutionResult(
        EnemyJabActionName,
        damageSettlement.DamageDealt,
        damageSettlement.TargetHpAfterDamage);
}

static DamageSettlementResult ResolveDamageSettlement(
    int baseAttackValue,
    int flatDamageModifier,
    int minimumDamage,
    int targetCurrentHp)
{
    var normalizedTargetCurrentHp = Math.Max(0, targetCurrentHp);
    var normalizedBaseAttackValue = Math.Max(1, baseAttackValue);
    var normalizedMinimumDamage = Math.Max(0, minimumDamage);
    var rawDamage = normalizedBaseAttackValue + flatDamageModifier;
    var adjustedDamage = Math.Max(normalizedMinimumDamage, rawDamage);
    var damageDealt = Math.Min(adjustedDamage, normalizedTargetCurrentHp);
    var targetHpAfterDamage = normalizedTargetCurrentHp - damageDealt;

    return new DamageSettlementResult(
        damageDealt,
        targetHpAfterDamage);
}

static bool ShouldTriggerEnemyExtraAction(CurrentEnemyState enemy, int enemyCurrentHp)
{
    var normalizedEnemyCurrentHp = Math.Max(0, enemyCurrentHp);
    var normalizedEnemyMaxHp = Math.Max(1, enemy.MaxHp);
    return normalizedEnemyCurrentHp > normalizedEnemyMaxHp / 2;
}

static PlayerActionResultDto ToPlayerActionResultDto(PlayerSkillExecutionResult skillResult) =>
    new(
        skillResult.SkillName,
        skillResult.DamageDealt,
        skillResult.EnemyHpAfterAction);

static EnemyActionResultDto ToEnemyActionResultDto(EnemyActionExecutionResult actionResult) =>
    new(
        actionResult.ActionName,
        actionResult.DamageDealt,
        actionResult.PlayerHpAfterAction);

static ResourceDeltaDto CreateResourceDelta(
    int goldDelta,
    int experienceDelta,
    int foodDelta) =>
    new(
        GoldDelta: goldDelta,
        ExperienceDelta: experienceDelta,
        FoodDelta: foodDelta);

static ResourceDeltaDto BuildShopPurchaseResourcesDelta(ShopItemDefinitionDto item) =>
    CreateResourceDelta(
        goldDelta: -item.GoldPrice,
        experienceDelta: 0,
        foodDelta: item.Effect.FoodDelta);

static ResourceDeltaDto BuildUseFoodResourcesDelta(int consumedAmount) =>
    CreateResourceDelta(
        goldDelta: 0,
        experienceDelta: 0,
        foodDelta: -consumedAmount);

static ResourceDeltaDto BuildConsumableUseResourcesDelta(string itemKey, int consumedAmount) =>
    itemKey.Equals(FoodItemKey, StringComparison.OrdinalIgnoreCase)
        ? BuildUseFoodResourcesDelta(consumedAmount)
        : CreateResourceDelta(
            goldDelta: 0,
            experienceDelta: 0,
            foodDelta: 0);

static HoldingDeltaDto BuildConsumableUseHoldingDelta(string itemKey, int consumedAmount) =>
    BuildHoldingDelta(itemKey, -consumedAmount);

static HoldingDeltaDto BuildHoldingDelta(string itemKey, int quantityDelta) =>
    new(
        ItemKey: itemKey,
        QuantityDelta: quantityDelta,
        DisplayName: ResolveHoldingDisplayName(itemKey));

static HoldingDeltaDto BuildShopPurchaseHoldingDelta(ShopItemDefinitionDto item) =>
    item.ItemKey.Equals(FoodItemKey, StringComparison.OrdinalIgnoreCase)
        ? BuildHoldingDelta(item.ItemKey, item.Effect.FoodDelta)
        : BuildHoldingDelta(item.ItemKey, 1);

static FightRewardResultDto BuildFightRewardResultDto(FightSettlementResult settlementResult) =>
    new(
        settlementResult.GoldReward,
        settlementResult.ExpReward,
        settlementResult.FoodReward,
        CreateResourceDelta(
            goldDelta: settlementResult.GoldReward,
            experienceDelta: settlementResult.ExpReward,
            foodDelta: settlementResult.FoodReward));

static async Task<PlayerItemHolding> GetOrCreateAndPersistHoldingAsync(
    GameDbContext dbContext,
    Player player,
    string itemKey)
{
    var existingHolding = await dbContext.PlayerItemHoldings
        .FirstOrDefaultAsync(holding =>
            holding.PlayerId == player.Id
            && holding.ItemKey == itemKey);
    if (existingHolding is not null)
    {
        return existingHolding;
    }

    var newHolding = new PlayerItemHolding
    {
        PlayerId = player.Id,
        ItemKey = itemKey,
        // Transitional bridge: only food can be seeded from legacy Player.Food during migration to holdings.
        Quantity = itemKey.Equals(FoodItemKey, StringComparison.OrdinalIgnoreCase)
            ? Math.Max(0, player.Food)
            : 0
    };
    dbContext.PlayerItemHoldings.Add(newHolding);
    await dbContext.SaveChangesAsync();
    return newHolding;
}

static async Task<PlayerItemHolding> GetOrCreateAndPersistFoodHoldingAsync(
    GameDbContext dbContext,
    Player player) =>
    await GetOrCreateAndPersistHoldingAsync(dbContext, player, FoodItemKey);

static void UpdateItemHoldingQuantityInMemory(PlayerItemHolding holding, int quantityDelta)
{
    var nextQuantity = holding.Quantity + quantityDelta;
    if (nextQuantity < 0)
    {
        throw new InvalidOperationException($"Item holding quantity cannot be negative. ResultingQuantity={nextQuantity}, Delta={quantityDelta}, PlayerId={holding.PlayerId}, ItemKey={holding.ItemKey}");
    }

    holding.Quantity = nextQuantity;
}

static void SyncFoodProjection(Player player, PlayerItemHolding foodHolding)
{
    player.Food = foodHolding.Quantity;
}

static async Task SyncFoodProjectionFromHoldingAsync(GameDbContext dbContext, Player player)
{
    var foodHolding = await GetOrCreateAndPersistFoodHoldingAsync(dbContext, player);
    SyncFoodProjection(player, foodHolding);
}

static async Task<PlayerDto> BuildPlayerDtoWithFoodProjectionAsync(GameDbContext dbContext, Player player)
{
    await SyncFoodProjectionFromHoldingAsync(dbContext, player);
    return ToPlayerDto(player);
}

static async Task<IReadOnlyList<PlayerItemHoldingDto>> BuildPlayerItemHoldingDtosAsync(GameDbContext dbContext, Player player)
{
    await SyncFoodProjectionFromHoldingAsync(dbContext, player);

    var holdings = await dbContext.PlayerItemHoldings
        .Where(holding => holding.PlayerId == player.Id && holding.Quantity > 0)
        .OrderBy(holding => holding.ItemKey)
        .ToListAsync();

    return holdings
        .Select(holding => ToPlayerItemHoldingDto(holding))
        .ToArray();
}

static PlayerItemHoldingDto ToPlayerItemHoldingDto(PlayerItemHolding holding) =>
    new(
        holding.ItemKey,
        holding.Quantity,
        ResolveHoldingDisplayName(holding.ItemKey));

static string ResolveHoldingDisplayName(string itemKey) =>
    itemKey.Equals(FoodItemKey, StringComparison.OrdinalIgnoreCase)
        ? "Food"
        : (itemKey.Equals(PotionItemKey, StringComparison.OrdinalIgnoreCase)
            ? "Potion"
            : itemKey);

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

file sealed record EnemyActionExecutionResult(
    string ActionName,
    int DamageDealt,
    int PlayerHpAfterAction);

file sealed record DamageSettlementResult(
    int DamageDealt,
    int TargetHpAfterDamage);

file sealed record EnemyDefeatSettlementResult(
    int GoldReward,
    int ExpReward,
    int FoodReward,
    bool LeveledUp);

file sealed record FightSettlementResult(
    FightSettlementBranch Branch,
    int GoldReward,
    int ExpReward,
    int FoodReward,
    bool LeveledUp);

file enum FightSettlementBranch
{
    EnemyDefeated,
    PlayerDefeated,
    Ongoing
}


file enum PlayerTurnActionType
{
    PowerStrike,
    BasicAttack
}

file enum EnemyTurnActionType
{
    EnemyJab,
    EnemyAttack
}

file sealed record ConsumableUseExecutionResult(
    bool Success,
    int StatusCode,
    string Message,
    UseItemResultDto? UseItemResult)
{
    public static ConsumableUseExecutionResult Succeed(UseItemResultDto result) =>
        new(
            Success: true,
            StatusCode: StatusCodes.Status200OK,
            Message: string.Empty,
            UseItemResult: result);

    public static ConsumableUseExecutionResult NotFound(string message) =>
        new(
            Success: false,
            StatusCode: StatusCodes.Status404NotFound,
            Message: message,
            UseItemResult: null);

    public static ConsumableUseExecutionResult BadRequest(string message) =>
        new(
            Success: false,
            StatusCode: StatusCodes.Status400BadRequest,
            Message: message,
            UseItemResult: null);
}

file sealed record PlayerTurnExecutionResult(
    List<PlayerActionResultDto> Actions,
    int EnemyHpAfterTurn,
    int TotalDamageDealt);

file sealed record EnemyTurnExecutionResult(
    List<EnemyActionResultDto> Actions,
    int PlayerHpAfterTurn,
    int TotalDamageDealt);
