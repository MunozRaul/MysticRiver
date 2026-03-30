namespace MysticRiver.Domain;

public class BattleResult
{
    public Creature Winner { get; }
    public Creature Loser { get; }

    public BattleResult(Creature winner, Creature loser)
    {
        Winner = winner;
        Loser = loser;
    }
}
