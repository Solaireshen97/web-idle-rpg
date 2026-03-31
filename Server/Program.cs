using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Players;

const int GoldIncrementAmount = 10;
const int ExpPerLevel = 10;
const int LevelUpAttackBonus = 1;
const int LevelUpMaxHpBonus = 5;

var enemyTemplates = new[]
{
    new { Name = "Training Slime", MaxHp = 8, Attack = 2, GoldReward = 5, ExpReward = 5 },
    new { Name = "Wolf", MaxHp = 12, Attack = 3, GoldReward = 8, ExpReward = 7 },
    new { Name = "Goblin", MaxHp = 15, Attack = 4, GoldReward = 10, ExpReward = 10 },
};

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

app.MapPost("/api/players/{id:int}/fight", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    var enemy = enemyTemplates[Random.Shared.Next(enemyTemplates.Length)];

    var playerAttack = Math.Max(1, player.Attack);
    var playerMaxHp = Math.Max(1, player.MaxHp);
    var playerCurrentHp = Math.Min(playerMaxHp, Math.Max(0, player.CurrentHp));
    var playerHpBeforeFight = playerCurrentHp;

    var enemyHp = enemy.MaxHp;
    while (playerCurrentHp > 0 && enemyHp > 0)
    {
        enemyHp -= playerAttack;
        if (enemyHp <= 0)
        {
            break;
        }

        playerCurrentHp -= enemy.Attack;
    }

    var isVictory = enemyHp <= 0;
    var goldReward = isVictory ? enemy.GoldReward : 0;
    var expReward = isVictory ? enemy.ExpReward : 0;
    var leveledUp = false;
    if (isVictory)
    {
        player.Gold += goldReward;
        player.Experience += expReward;
        player.CurrentHp = playerCurrentHp;

        while (player.Experience >= ExpPerLevel)
        {
            player.Experience -= ExpPerLevel;
            player.Level += 1;
            player.Attack += LevelUpAttackBonus;
            player.MaxHp += LevelUpMaxHpBonus;
            leveledUp = true;
        }

        if (leveledUp)
        {
            player.CurrentHp = player.MaxHp;
        }
    }
    else
    {
        player.CurrentHp = playerMaxHp;
    }

    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    var summary = isVictory
        ? $"{player.Name} defeated {enemy.Name} and earned {goldReward} gold, {expReward} EXP. HP: {playerHpBeforeFight}->{player.CurrentHp}."
            + (leveledUp ? $" Level up! Now Lv{player.Level}." : "")
        : $"{player.Name} was defeated by {enemy.Name}. HP reset to {player.CurrentHp}/{playerMaxHp}.";

    return Results.Ok(new FightResultDto(
        isVictory,
        goldReward,
        expReward,
        leveledUp,
        enemy.Name,
        enemy.MaxHp,
        enemy.Attack,
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
        player.Attack,
        player.MaxHp,
        player.CurrentHp,
        player.CreatedAt,
        player.UpdatedAt);
