namespace Shared.Players;

public sealed record PlayerDto(
    int Id,
    string Name,
    int Gold,
    int Attack,
    int MaxHp,
    int CurrentHp,
    int Level,
    int Experience,
    DateTime CreatedAt,
    DateTime UpdatedAt);
