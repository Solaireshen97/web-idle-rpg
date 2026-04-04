namespace Shared.Players;

public sealed record HoldingDeltaDto(
    string ItemKey,
    int QuantityDelta,
    string DisplayName);
