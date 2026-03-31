namespace Server.Data;

public class Player
{
    private const int DefaultAttack = 3;
    private const int DefaultMaxHp = 30;

    public Player()
    {
        var now = DateTime.UtcNow;
        CreatedAt = now;
        UpdatedAt = now;
        Attack = DefaultAttack;
        MaxHp = DefaultMaxHp;
        CurrentHp = DefaultMaxHp;
        Level = 1;
    }

    public int Id { get; set; }
    public required string Name { get; set; }
    public int Gold { get; set; }
    public int Attack { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Level { get; set; }
    public int Experience { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
