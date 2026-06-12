namespace MysticRiver.Domain;

/// <summary>
/// Represents battle state after a single turn.
/// </summary>
public sealed class TurnResult {
    public int Creature1Hp { get; }
    public int Creature2Hp { get; }
    public StatusEffect Creature1Status { get; }
    public StatusEffect Creature2Status { get; }
    public CrowdControlKind Creature1CrowdControl { get; }
    public CrowdControlKind Creature2CrowdControl { get; }
    public bool BattleEnded { get; }
    public BattleResult? FinalResult { get; }

    /// <summary>
    /// Creates a turn result from post-turn HP/status values and optional final battle result.
    /// </summary>
    public TurnResult(
        int creature1Hp,
        int creature2Hp,
        StatusEffect creature1Status = StatusEffect.None,
        StatusEffect creature2Status = StatusEffect.None,
        CrowdControlKind creature1CrowdControl = CrowdControlKind.None,
        CrowdControlKind creature2CrowdControl = CrowdControlKind.None,
        BattleResult? finalResult = null) {
        ArgumentOutOfRangeException.ThrowIfNegative(creature1Hp);
        ArgumentOutOfRangeException.ThrowIfNegative(creature2Hp);

        Creature1Hp = creature1Hp;
        Creature2Hp = creature2Hp;
        Creature1Status = creature1Status;
        Creature2Status = creature2Status;
        Creature1CrowdControl = creature1CrowdControl;
        Creature2CrowdControl = creature2CrowdControl;
        FinalResult = finalResult;
        BattleEnded = finalResult is not null;
    }
}
