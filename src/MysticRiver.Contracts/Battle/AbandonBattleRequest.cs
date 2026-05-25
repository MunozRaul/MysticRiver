namespace MysticRiver.Contracts.Battle;

public sealed record AbandonBattleRequest(
    string AbandoningCreatureId = "player");
