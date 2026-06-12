namespace MysticRiver.Contracts.Battle;

public sealed record JoinMatchResponse(
    string BattleId,
    MatchStatus MatchStatus,
    string HostPlayerId,
    string GuestPlayerId,
    string HostCreatureId,
    string GuestCreatureId,
    BattleStateDto State);
