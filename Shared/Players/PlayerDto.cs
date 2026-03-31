namespace Shared.Players;

public sealed record PlayerDto(
    int Id,
    string Name,
    int Gold,
    int Attack,
    int MaxHp,
    int CurrentHp,
    DateTime CreatedAt,
    DateTime UpdatedAt);
