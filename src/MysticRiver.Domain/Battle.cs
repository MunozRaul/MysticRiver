namespace MysticRiver.Domain;

public sealed class Battle
{
    public Creature Player1 { get; }
    public Creature Player2 { get; }

    public bool IsOver => Player1.IsDead || Player2.IsDead;

    public Battle(Creature player1, Creature player2)
    {
        Player1 = player1;
        Player2 = player2;
    }

    /// <summary>
    /// Applies damage from the attacker to the target.
    /// Throws <see cref="InvalidOperationException"/> if the battle is already over.
    /// </summary>
    public void ExecuteAction(Creature attacker, Creature target, int damage)
    {
        if (IsOver)
        {
            throw new InvalidOperationException("No further turns are allowed: the battle is already over.");
        }

        if (attacker != Player1 && attacker != Player2)
        {
            throw new ArgumentException("Attacker does not belong to this battle.", nameof(attacker));
        }

        if (target != Player1 && target != Player2)
        {
            throw new ArgumentException("Target does not belong to this battle.", nameof(target));
        }

        if (attacker == target)
        {
            throw new ArgumentException("Attacker and target must be different creatures.", nameof(target));
        }

        target.TakeDamage(damage);
    }

    /// <summary>
    /// Returns the battle result when the battle is over, or <c>null</c> if it is still in progress.
    /// </summary>
    public BattleResult? GetResult()
    {
        if (Player1.IsDead)
        {
            return new BattleResult(winner: Player2, loser: Player1);
        }

        if (Player2.IsDead)
        {
            return new BattleResult(winner: Player1, loser: Player2);
        }

        return null;
    }
}
