namespace MysticRiver.Domain;

/// <summary>
/// Represents battle state after a single turn.
/// </summary>
public sealed class TurnResult
{
    public int Player1Hp { get; }
    public int Player2Hp { get; }
    public bool BattleEnded { get; }
    public BattleResult? FinalResult { get; }

    /// <summary>
    /// Creates a turn result from post-turn HP values and optional final battle result.
    /// </summary>
    public TurnResult(int player1Hp, int player2Hp, BattleResult? finalResult = null)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(player1Hp);
        ArgumentOutOfRangeException.ThrowIfNegative(player2Hp);

        Player1Hp = player1Hp;
        Player2Hp = player2Hp;
        FinalResult = finalResult;
        BattleEnded = finalResult is not null;
    }
}