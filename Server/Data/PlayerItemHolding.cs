namespace Server.Data;

public class PlayerItemHolding
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public required string ItemKey { get; set; }
    public int Quantity { get; set; }
}
