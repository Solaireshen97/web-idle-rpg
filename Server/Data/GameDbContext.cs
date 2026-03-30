using Microsoft.EntityFrameworkCore;

namespace Server.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
}
