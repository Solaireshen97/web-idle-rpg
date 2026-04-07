namespace Shared.Players;

public sealed record PlayerItemHoldingDto(
    string ItemKey,
    int Quantity,
    string DisplayName);
