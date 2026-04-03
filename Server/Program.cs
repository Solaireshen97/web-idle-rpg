using System.Data;
using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Players;
using Shared.Shop;

const int GoldIncrementAmount = 10;
const int FoodHealAmount = 10;
const int BaseExpPerLevel = 10;
const int ExpPerLevelGrowth = 5;
const int LevelUpAttackBonus = 1;
const int LevelUpMaxHpBonus = 5;
const int DefeatSurvivalHp = 1;
const int PowerStrikeBonusDamage = 1;
const int PowerStrikeCooldownTurns = 2;
const string PowerStrikeSkillName = "Power Strike";
const string BasicAttackSkillName = "Basic Attack";
const string EnemyJabActionName = "Enemy Jab";
const string EnemyAttackActionName = "Enemy Attack";
const string PlayersTableName = "Players";
const string PreferredEnemyRandomKey = "random";
const string PreferredEnemyTrainingSlimeKey = "training-slime";
const string PreferredEnemyWolfKey = "wolf";
const string PreferredEnemyGoblinKey = "goblin";

var enemyTemplateByKey = new Dictionary<string, EnemyTemplate>(StringComparer.OrdinalIgnoreCase)
{
    [PreferredEnemyTrainingSlimeKey] = new EnemyTemplate("Training Slime", 24, 2, 5, 5),
    [PreferredEnemyWolfKey] = new EnemyTemplate("Wolf", 36, 3, 9, 8),
    [PreferredEnemyGoblinKey] = new EnemyTemplate("Goblin", 52, 4, 12, 11),
};

var enemyTemplates = enemyTemplateByKey.Values.ToArray();
var shopItems = new[]
{
    new ShopItemDefinitionDto(
        ItemKey: "food",
        DisplayName: "Food",
        GoldPrice: 5,
        Effect: new ShopItemEffectDto(FoodDelta: 1)),
    new ShopItemDefinitionDto(
        ItemKey: "food-pack",
        DisplayName: "Food Pack",
        GoldPrice: 12,
        Effect: new ShopItemEffectDto(FoodDelta: 3)),
    new ShopItemDefinitionDto(
        ItemKey: "food-crate",
        DisplayName: "Food Crate",
        GoldPrice: 18,
        Effect: new ShopItemEffectDto(FoodDelta: 5))
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
}

app.MapGet("/api/ping", () => Results.Ok(new { message = "pong" }));

app.MapGet("/api/shop/items", () => Results.Ok(shopItems));
app.MapGet("/api/shop/items/food", () => Results.Ok(shopItemByKey["food"]));

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
    return Results.Ok(ToPlayerDto(player));
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
    var settlementResult = ApplyFightRoundSettlement(
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

    return Results.Ok(new FightResultDto(
        enemyDefeated,
        settlementResult.GoldReward,
        settlementResult.ExpReward,
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

static int GetRequiredExpForNextLevel(int currentLevel) =>
    BaseExpPerLevel + (currentLevel - 1) * ExpPerLevelGrowth;

static FightSettlementResult ApplyFightRoundSettlement(
    Player player,
    CurrentEnemyState enemy,
    int enemyCurrentHp,
    int playerCurrentHp,
    bool enemyDefeated,
    bool playerDefeated)
{
    if (enemyDefeated)
    {
        var enemyDefeatSettlementResult = ApplyEnemyDefeatSettlement(player, enemy, playerCurrentHp);
        ClearCurrentEnemy(player);
        return new FightSettlementResult(
            FightSettlementBranch.EnemyDefeated,
            enemyDefeatSettlementResult.GoldReward,
            enemyDefeatSettlementResult.ExpReward,
            enemyDefeatSettlementResult.LeveledUp);
    }

    if (playerDefeated)
    {
        player.CurrentHp = DefeatSurvivalHp;
        ClearCurrentEnemy(player);
        return new FightSettlementResult(
            FightSettlementBranch.PlayerDefeated,
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
        false);
}

static EnemyDefeatSettlementResult ApplyEnemyDefeatSettlement(
    Player player,
    CurrentEnemyState enemy,
    int playerCurrentHp)
{
    var goldReward = enemy.GoldReward;
    var expReward = enemy.ExperienceReward;
    player.Gold += goldReward;
    player.Experience += expReward;
    player.Food += 1;
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
            $"{player.Name} defeated {enemy.Name} and earned {settlementResult.GoldReward} gold, {settlementResult.ExpReward} EXP, 1 Food."
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
    AddPlayerColumnIfMissing(dbContext, existingColumns, "PreferredEnemyKey");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "PowerStrikeEnabled");
    AddPlayerColumnIfMissing(dbContext, existingColumns, "PowerStrikeCooldownRemaining");
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

    await dbContext.SaveChangesAsync();
    return Results.Ok(new ShopPurchaseResultDto(
        item.ItemKey,
        item.DisplayName,
        item.GoldPrice,
        item.Effect,
        ToPlayerDto(player)));
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
    ApplyShopItemEffect(player, item.Effect);
    player.UpdatedAt = DateTime.UtcNow;
}

static void ApplyShopItemEffect(Player player, ShopItemEffectDto effect)
{
    player.Food += effect.FoodDelta;
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
    bool LeveledUp);

file sealed record FightSettlementResult(
    FightSettlementBranch Branch,
    int GoldReward,
    int ExpReward,
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

file sealed record PlayerTurnExecutionResult(
    List<PlayerActionResultDto> Actions,
    int EnemyHpAfterTurn,
    int TotalDamageDealt);

file sealed record EnemyTurnExecutionResult(
    List<EnemyActionResultDto> Actions,
    int PlayerHpAfterTurn,
    int TotalDamageDealt);
