namespace MysticRiver.Domain;

public sealed class Battle
{
    public Creature Creature1 { get; }
    public Creature Creature2 { get; }

    public bool IsOver => Creature1.IsDead || Creature2.IsDead;

    public Battle(Creature creature1, Creature creature2)
    {
        ArgumentNullException.ThrowIfNull(creature1);
        ArgumentNullException.ThrowIfNull(creature2);

        if (ReferenceEquals(creature1, creature2))
        {
            throw new ArgumentException("creature1 and creature2 must be different instances.");
        }

        Creature1 = creature1;
        Creature2 = creature2;
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

        if (attacker != Creature1 && attacker != Creature2)
        {
            throw new ArgumentException("Attacker does not belong to this battle.", nameof(attacker));
        }

        if (target != Creature1 && target != Creature2)
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
        if (Creature1.IsDead)
        {
            result = new BattleResult(winner: Creature2, loser: Creature1);
            return true;
        }

        if (Creature2.IsDead)
        {
            result = new BattleResult(winner: Creature1, loser: Creature2);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Executes one deterministic turn with two moves.
    /// Creature1's move resolves first; Creature2's move resolves second unless the battle already ended.
    /// </summary>
    public TurnResult ExecuteTurn(Move a, Move b)
    {
        if (IsOver)
        {
            throw new InvalidOperationException("No further turns are allowed: the battle is already over.");
        }

        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        ValidateMove(a, nameof(a));
        ValidateMove(b, nameof(b));

        if (a.Attacker == b.Attacker)
        {
            throw new ArgumentException("Each move must have a different attacker.");
        }

        var (first, second) = DetermineMoveOrder(a, b);

        ApplyMoveIfPossible(first);
        ApplyMoveIfPossible(second);

        TryGetResult(out var outcome);
        return new TurnResult(
            creature1Hp: Creature1.CurrentHp,
            creature2Hp: Creature2.CurrentHp,
            finalResult: outcome);
    }

    private (Move first, Move second) DetermineMoveOrder(Move a, Move b)
    {
        if (a.Attacker.Initiative > b.Attacker.Initiative)
        {
            return (a, b);
        }
        else if (b.Attacker.Initiative > a.Attacker.Initiative)
        {
            return (b, a);
        }
        else
        {
            // If initiatives are equal, default Creature1 going first (deterministic tiebreaker)
            return ReferenceEquals(a.Attacker, Creature1) ? (a, b) : (b, a);
        }
    }

    private void ValidateMove(Move move, string paramName)
    {
        if (move.Attacker != Creature1 && move.Attacker != Creature2)
        {
            throw new ArgumentException("Attacker does not belong to this battle.", paramName);
        }

        if (move.Target != Creature1 && move.Target != Creature2)
        {
            throw new ArgumentException("Target does not belong to this battle.", paramName);
        }

        if (move.Attacker == move.Target)
        {
            throw new ArgumentException("Attacker and target must be different creatures.", paramName);
        }
    }

    private void ApplyMoveIfPossible(Move move)
    {
        if (IsOver || move.Attacker.IsDead)
        {
            return; // Skip if battle is already over or attacker is dead
        }

        ExecuteAction(move.Attacker, move.Target, move.ResolveDamage());
    }
}
