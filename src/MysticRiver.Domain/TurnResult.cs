namespace MysticRiver.Domain;

/// <summary>
/// Represents battle state after a single turn.
/// </summary>
public sealed class TurnResult
{
    public int Creature1Hp { get; }
    public int Creature2Hp { get; }
    public bool BattleEnded { get; }
    public BattleResult? FinalResult { get; }

    /// <summary>
    /// Creates a turn result from post-turn HP values and optional final battle result.
    /// </summary>
    public TurnResult(int creature1Hp, int creature2Hp, BattleResult? finalResult = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(creature1Hp);
        ArgumentOutOfRangeException.ThrowIfNegative(creature2Hp);

        Creature1Hp = creature1Hp;
        Creature2Hp = creature2Hp;
        FinalResult = finalResult;
        BattleEnded = finalResult is not null;
    }
}