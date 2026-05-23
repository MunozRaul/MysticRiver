namespace MysticRiver.Contracts.Battle;

public sealed record BattleActionSummaryDto(
    AbilityDefinitionDto Ability,
    string ActorId,
    string? TargetId,
    IReadOnlyList<AppliedEffectDto> AppliedEffects);
