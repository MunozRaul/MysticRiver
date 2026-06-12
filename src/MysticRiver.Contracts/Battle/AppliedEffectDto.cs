namespace MysticRiver.Contracts.Battle;

public sealed record AppliedEffectDto(
    AppliedEffectKind Kind,
    string TargetId,
    int? Amount = null,
    double? Ratio = null,
    StatusEffect? StatusEffect = null,
    CrowdControlKind? CrowdControl = null,
    int? Turns = null);
