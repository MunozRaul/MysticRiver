namespace MysticRiver.Contracts.Battle;

public sealed record BattleStateDto(
    string BattleId,
    int RoundNumber,
    int StateVersion,
    BattleCreatureDto Creature1,
    BattleCreatureDto Creature2,
    bool BattleEnded,
    string? WinnerCreatureId,
    MatchStatus MatchStatus = MatchStatus.InProgress);
