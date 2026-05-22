namespace MysticRiver.Contracts.Battle;

public sealed record BattleCreatureDto(
    string CreatureId,
    string Name,
    int MaxHp,
    int CurrentHp,
    int MaxMana,
    int CurrentMana,
    int Initiative,
    int CurrentShield,
    IReadOnlyList<StatusEffectStateDto> StatusEffects,
    CrowdControlKind CrowdControl,
    int CrowdControlTurnsRemaining,
    bool IsDead);
