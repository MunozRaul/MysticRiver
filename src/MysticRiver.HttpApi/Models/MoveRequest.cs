namespace MysticRiver.HttpApi.Models;

// Request payload for submitting a player's move to the API.
public class MoveRequest
{
    public required string PlayerId { get; init; }
    public required string Move { get; init; }
}
