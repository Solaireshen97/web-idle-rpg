namespace Shared.Players;

public sealed record PlayerDto(
    int Id,
    string Name,
    int Gold,
    int Level,
    int Experience,
    int Attack,
    int MaxHp,
    int CurrentHp,
    DateTime CreatedAt,
    DateTime UpdatedAt);
