namespace MysticRiver.Contracts.Battle;

public sealed record CreateMatchResponse(
    string BattleId,
    MatchStatus MatchStatus,
    string HostPlayerId,
    string HostCreatureId,
    BattleStateDto State);
