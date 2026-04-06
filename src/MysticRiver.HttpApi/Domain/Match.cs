namespace MysticRiver.HttpApi.Domain;

// Represents the mutable in-memory state of a running match.
public class Match
{
    public required string Id { get; init; }
    public required List<Player> Players { get; init; }
    public required List<MoveEntry> Moves { get; init; }
    public string? CurrentTurnPlayerId { get; set; }
    public DateTime? LastMoveAtUtc { get; set; }
}
