namespace MysticRiver.Contracts.Battle;

public sealed record BattleStateDto(
    string BattleId,
    int RoundNumber,
    BattleCreatureDto Creature1,
    BattleCreatureDto Creature2,
    bool BattleEnded,
    string? WinnerCreatureId);
