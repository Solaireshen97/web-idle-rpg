namespace Server.Data;

public class ActiveEnemy
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public required string Name { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Attack { get; set; }
    public int GoldReward { get; set; }
    public int ExpReward { get; set; }
}
