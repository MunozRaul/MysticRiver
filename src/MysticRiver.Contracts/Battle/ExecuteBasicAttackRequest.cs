namespace MysticRiver.Contracts.Battle;

public sealed record ExecuteBasicAttackRequest(
    string AttackerId = "player",
    string TargetId = "enemy",
    int Power = 20);
