namespace MysticRiver.Domain;

public sealed class BattleResult
{
    public Creature Winner { get; }
    public Creature Loser { get; }

    public BattleResult(Creature winner, Creature loser)
    {
        ArgumentNullException.ThrowIfNull(winner);
        ArgumentNullException.ThrowIfNull(loser);
        Winner = winner;
        Loser = loser;
    }
}
