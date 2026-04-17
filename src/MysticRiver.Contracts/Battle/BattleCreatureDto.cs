namespace MysticRiver.Contracts.Battle;

public sealed record BattleCreatureDto(
    string CreatureId,
    string Name,
    int MaxHp,
    int CurrentHp,
    int Initiative,
    bool IsDead);
