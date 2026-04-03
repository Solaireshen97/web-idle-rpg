namespace Shared.Players;

public sealed record ResourceDeltaDto(
    int GoldDelta,
    int ExperienceDelta,
    int FoodDelta);
