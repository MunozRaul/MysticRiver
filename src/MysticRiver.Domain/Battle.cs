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
            return new BattleResult(winner: Player1, loser: Player2);
        }

        return null;
    }

    /// <summary>
    /// Executes one deterministic turn with two moves.
    /// Player1's move resolves first; Player2's move resolves second unless the battle already ended.
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

        var first = a.Attacker == Player1 ? a : b;
        var second = first == a ? b : a;

        ApplyMoveIfPossible(first);
        ApplyMoveIfPossible(second);

        var outcome = GetResult();
        return new TurnResult(
            player1Hp: Player1.CurrentHp,
            player2Hp: Player2.CurrentHp,
            finalResult: outcome);
    }

    private void ValidateMove(Move move, string paramName)
    {
        if (move.Attacker != Player1 && move.Attacker != Player2)
        {
            throw new ArgumentException("Attacker does not belong to this battle.", paramName);
        }

        if (move.Target != Player1 && move.Target != Player2)
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
            result = new BattleResult(winner: Player1, loser: Player2);
            return true;
        }

        result = null;
        return false;
    }
}
