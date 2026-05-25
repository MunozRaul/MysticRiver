namespace MysticRiver.Contracts.Battle;

public sealed record JoinMatchRequest(
    string GuestPlayerId,
    string? GuestDisplayName = null);
