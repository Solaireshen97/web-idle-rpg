using Microsoft.EntityFrameworkCore;
using Server.Data;
using Shared.Players;

const int GoldIncrementAmount = 10;
const int FightEnemyMaxHp = 8;
const int FightEnemyAttack = 2;
const int FightVictoryGoldReward = 5;

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

    var playerAttack = Math.Max(1, player.Attack);
    player.MaxHp = Math.Max(1, player.MaxHp);
    player.CurrentHp = Math.Min(player.MaxHp, Math.Max(0, player.CurrentHp));

    var enemyHp = FightEnemyMaxHp;
    while (player.CurrentHp > 0 && enemyHp > 0)
    {
        enemyHp -= playerAttack;
        if (enemyHp <= 0)
        {
            break;
        }

        player.CurrentHp -= FightEnemyAttack;
    }

    var isVictory = enemyHp <= 0;
    var goldReward = isVictory ? FightVictoryGoldReward : 0;
    if (isVictory)
    {
        player.Gold += goldReward;
    }
    else
    {
        player.CurrentHp = player.MaxHp;
    }

    player.UpdatedAt = DateTime.UtcNow;

    await dbContext.SaveChangesAsync();

    return Results.Ok(new FightResultDto(
        isVictory,
        goldReward,
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
        player.CreatedAt,
        player.UpdatedAt);
