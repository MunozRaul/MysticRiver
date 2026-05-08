namespace MysticRiver.Domain;

<<<<<<< HEAD
public sealed class BattleResult {
    public Creature Winner { get; }
    public Creature Loser { get; }

    public BattleResult(Creature winner, Creature loser) {
=======
public sealed class BattleResult
{
    public Creature Winner { get; }
    public Creature Loser { get; }

    public BattleResult(Creature winner, Creature loser)
    {
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
        ArgumentNullException.ThrowIfNull(winner);
        ArgumentNullException.ThrowIfNull(loser);
        Winner = winner;
        Loser = loser;
    }
}
