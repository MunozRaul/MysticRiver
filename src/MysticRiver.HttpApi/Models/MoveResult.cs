namespace MysticRiver.HttpApi.Models;

// Response payload returned after a move was validated and stored.
public class MoveResult
{
    public required string MatchId { get; init; }
    public bool Accepted { get; init; }
    public required string LastMove { get; init; }
    public string? NextPlayerId { get; init; }
    public required IReadOnlyList<MoveResultEntry> Moves { get; init; }
}
