namespace Server.Data;

public class Player
{
    public Player()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public int Gold { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
