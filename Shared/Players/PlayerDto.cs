namespace Shared.Players;

public sealed record PlayerDto(
    int Id,
    string Name,
    int Gold,
    DateTime CreatedAt,
    DateTime UpdatedAt);
