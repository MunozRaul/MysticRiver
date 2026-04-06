namespace MysticRiver.HttpApi.Models;

// Describes one move entry included in the API response history.
public class MoveResultEntry
{
    public required string PlayerId { get; init; }
    public required string Move { get; init; }
    public DateTime TimestampUtc { get; init; }
}
