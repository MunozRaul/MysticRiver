namespace MysticRiver.Contracts.Battle;

public sealed record StatusEffectStateDto(
    StatusEffect Effect,
    int Stacks,
    int RemainingTurns);
