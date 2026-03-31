using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Players;

const int GoldIncrementAmount = 10;
const int ExpPerLevel = 10;

EnemyTemplate[] enemyTemplates =
[
    new("Training Slime", 8, 2, 3, 5),
    new("Wolf", 14, 4, 6, 10),
    new("Goblin", 12, 3, 5, 8),
];

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

app.MapPost("/api/players/{id:int}/rest", async (GameDbContext dbContext, int id) =>
{
    var player = await dbContext.Players.FindAsync(id);
    if (player is null)
    {
        return Results.NotFound();
    }

    player.CurrentHp = player.MaxHp;
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

    var activeEnemy = await dbContext.ActiveEnemies
        .FirstOrDefaultAsync(e => e.PlayerId == id);

    if (activeEnemy is null)
    {
        var template = enemyTemplates[Random.Shared.Next(enemyTemplates.Length)];
        activeEnemy = new ActiveEnemy
        {
            PlayerId = id,
            Name = template.Name,
            MaxHp = template.MaxHp,
            CurrentHp = template.MaxHp,
            Attack = template.Attack,
            GoldReward = template.GoldReward,
            ExpReward = template.ExpReward,
        };
        dbContext.ActiveEnemies.Add(activeEnemy);
        await dbContext.SaveChangesAsync();
    }

    var playerAttack = Math.Max(1, player.Attack);

    // Player attacks enemy for one round
    activeEnemy.CurrentHp -= playerAttack;

    var isVictory = activeEnemy.CurrentHp <= 0;
    var goldReward = 0;
    var expReward = 0;
    var leveledUp = false;
    string summary;

    if (isVictory)
    {
        goldReward = activeEnemy.GoldReward;
        expReward = activeEnemy.ExpReward;
        player.Gold += goldReward;
        player.Experience += expReward;

        while (player.Experience >= ExpPerLevel)
        {
            player.Experience -= ExpPerLevel;
            player.Level++;
            player.Attack++;
            player.MaxHp += 5;
            player.CurrentHp = player.MaxHp;
            leveledUp = true;
        }

        summary = $"{player.Name} defeated {activeEnemy.Name} and earned {goldReward} gold and {expReward} EXP."
            + (leveledUp ? $" Level up! Now Lv{player.Level}." : "");

        dbContext.ActiveEnemies.Remove(activeEnemy);
    }
    else
    {
        // Enemy counterattacks
        player.CurrentHp = Math.Max(0, player.CurrentHp - activeEnemy.Attack);

        summary = $"{player.Name} hit {activeEnemy.Name} for {playerAttack} (HP {activeEnemy.CurrentHp}/{activeEnemy.MaxHp})."
            + $" {activeEnemy.Name} hit back for {activeEnemy.Attack} (Player HP {player.CurrentHp}/{player.MaxHp}).";
    }

    player.UpdatedAt = DateTime.UtcNow;
    await dbContext.SaveChangesAsync();

    return Results.Ok(new FightResultDto(
        isVictory,
        goldReward,
        expReward,
        leveledUp,
        activeEnemy.Name,
        activeEnemy.MaxHp,
        Math.Max(0, activeEnemy.CurrentHp),
        activeEnemy.Attack,
        summary,
        ToPlayerDto(player)));
});

app.Run();

static PlayerDto ToPlayerDto(Player player) =>
    new(
        player.Id,
        player.Name,
        player.Gold,
        player.Attack,
        player.MaxHp,
        player.CurrentHp,
        player.Level,
        player.Experience,
        player.CreatedAt,
        player.UpdatedAt);
