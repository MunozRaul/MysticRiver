namespace MysticRiver.Domain;

/// <summary>
/// Represents battle state after a single turn.
/// </summary>
<<<<<<< HEAD
public sealed class TurnResult {
    public int Creature1Hp { get; }
    public int Creature2Hp { get; }
    public StatusEffect? Creature1Status { get; }
    public StatusEffect? Creature2Status { get; }
    public bool BattleEnded { get; }
    public BattleResult? FinalResult { get; }

    /// <summary>
    /// Creates a turn result from post-turn HP/status values and optional final battle result.
    /// </summary>
    public TurnResult(
        int creature1Hp,
        int creature2Hp,
        StatusEffect? creature1Status = null,
        StatusEffect? creature2Status = null,
        BattleResult? finalResult = null) {
=======
public sealed class TurnResult
{
    public int Creature1Hp { get; }
    public int Creature2Hp { get; }
    public bool BattleEnded { get; }
    public BattleResult? FinalResult { get; }
    public StatusEffect? Creature1Status { get; }
    public StatusEffect? Creature2Status { get; }

    /// <summary>
    /// Creates a turn result from post-turn HP values, status effects, and optional final battle result.
    /// </summary>
    public TurnResult(int creature1Hp, int creature2Hp, StatusEffect? creature1Status = null, StatusEffect? creature2Status = null, BattleResult? finalResult = null)
    {
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
        ArgumentOutOfRangeException.ThrowIfNegative(creature1Hp);
        ArgumentOutOfRangeException.ThrowIfNegative(creature2Hp);

        Creature1Hp = creature1Hp;
        Creature2Hp = creature2Hp;
        Creature1Status = creature1Status;
        Creature2Status = creature2Status;
        FinalResult = finalResult;
        BattleEnded = finalResult is not null;
    }
<<<<<<< HEAD
}
=======
}
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
