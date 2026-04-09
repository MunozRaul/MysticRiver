namespace MysticRiver.Domain;

public sealed class Battle
{
    public Creature Player1 { get; }
    public Creature Player2 { get; }

    public bool IsOver => Player1.IsDead || Player2.IsDead;

    public Battle(Creature player1, Creature player2)
    {
        ArgumentNullException.ThrowIfNull(player1);
        ArgumentNullException.ThrowIfNull(player2);

        if (ReferenceEquals(player1, player2))
        {
            throw new ArgumentException("player1 and player2 must be different instances.");
        }

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
    /// Returns <c>true</c> and sets <paramref name="result"/> when the battle is over;
    /// returns <c>false</c> and sets <paramref name="result"/> to <c>null</c> when still in progress.
    /// </summary>
    public bool TryGetResult(out BattleResult? result)
    {
        if (Player1.IsDead)
        {
            result = new BattleResult(winner: Player2, loser: Player1);
            return true;
        }

        if (Player2.IsDead)
        {
            result = new BattleResult(winner: Player1, loser: Player2);
            return true;
        }

        result = null;
        return false;
    }
}
