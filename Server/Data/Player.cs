namespace Server.Data;

public class Player
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Gold { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
