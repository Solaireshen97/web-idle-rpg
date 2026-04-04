namespace Server.Data;

/// <summary>
/// Minimal player item holding row for quantity-based ownership (itemKey + quantity).
/// </summary>
public class PlayerItemHolding
{
    /// <summary>
    /// Holding row identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Owner player identifier.
    /// </summary>
    public int PlayerId { get; set; }

    /// <summary>
    /// Logical item key (for example: "food", "potion").
    /// </summary>
    public required string ItemKey { get; set; }

    /// <summary>
    /// Owned quantity for the item key.
    /// </summary>
    public int Quantity { get; set; }
}
