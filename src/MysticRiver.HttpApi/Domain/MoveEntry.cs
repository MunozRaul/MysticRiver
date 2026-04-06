namespace MysticRiver.HttpApi.Domain;

// Stores one submitted move together with its author and timestamp.
public class MoveEntry
{
    public required string PlayerId { get; init; }
    public required string Move { get; init; }
    public DateTime TimestampUtc { get; init; }
}
