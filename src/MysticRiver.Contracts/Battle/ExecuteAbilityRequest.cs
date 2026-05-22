namespace MysticRiver.Contracts.Battle;

public sealed record ExecuteAbilityRequest(
    string AbilityId,
    string AttackerId = "player",
    string? TargetId = "enemy");
