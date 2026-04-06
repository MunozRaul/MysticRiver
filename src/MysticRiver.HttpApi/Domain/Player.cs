namespace MysticRiver.HttpApi.Domain;

// Identifies a player participating in a match.
public class Player
{
    public required string Id { get; init; }
    public required string Name { get; init; }
}
