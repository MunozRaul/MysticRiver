namespace MysticRiver.Contracts.Battle;

public sealed record StartBattleResponse(
    string BattleId,
    BattleStateDto State);
