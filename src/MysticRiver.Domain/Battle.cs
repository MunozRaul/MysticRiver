namespace MysticRiver.Domain;

<<<<<<< HEAD
public sealed class Battle {
=======
public sealed class Battle
{
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
    public Creature Creature1 { get; }
    public Creature Creature2 { get; }

    public bool IsOver => Creature1.IsDead || Creature2.IsDead;

<<<<<<< HEAD
    public Battle(Creature creature1, Creature creature2) {
        ArgumentNullException.ThrowIfNull(creature1);
        ArgumentNullException.ThrowIfNull(creature2);

        if (ReferenceEquals(creature1, creature2)) {
=======
    public Battle(Creature creature1, Creature creature2)
    {
        ArgumentNullException.ThrowIfNull(creature1);
        ArgumentNullException.ThrowIfNull(creature2);

        if (ReferenceEquals(creature1, creature2))
        {
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
            throw new ArgumentException("creature1 and creature2 must be different instances.");
        }

        Creature1 = creature1;
        Creature2 = creature2;
    }

    /// <summary>
<<<<<<< HEAD
    /// Returns <c>true</c> and sets <paramref name="result"/> when the battle is over;
    /// returns <c>false</c> and sets <paramref name="result"/> to <c>null</c> when still in progress.
    /// </summary>
    public bool TryGetResult(out BattleResult? result) {
        if (Creature1.IsDead) {
=======
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
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
            result = new BattleResult(winner: Creature2, loser: Creature1);
            return true;
        }

<<<<<<< HEAD
        if (Creature2.IsDead) {
=======
        if (Creature2.IsDead)
        {
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
            result = new BattleResult(winner: Creature1, loser: Creature2);
            return true;
        }

        result = null;
        return false;
    }

    /// <summary>
    /// Executes one deterministic turn with two moves.
<<<<<<< HEAD
    /// The higher-initiative creature's move resolves first; the second move is skipped if the battle ends after the first.
    /// </summary>
    public TurnResult ExecuteTurn(Move a, Move b) {
        if (IsOver) {
=======
    /// Creature1's move resolves first; Creature2's move resolves second unless the battle already ended.
    /// </summary>
    public TurnResult ExecuteTurn(Move a, Move b)
    {
        if (IsOver)
        {
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
            throw new InvalidOperationException("No further turns are allowed: the battle is already over.");
        }

        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        ValidateMove(a, nameof(a));
        ValidateMove(b, nameof(b));

<<<<<<< HEAD
        if (GetActor(a) == GetActor(b)) {
            throw new ArgumentException("Each move must have a different actor.");
        }

        // Tick status effects before resolving actions so effects apply on subsequent turns.
        Creature1.ApplyEndOfTurnEffects();
        if (!IsOver) {
=======
        if (a.Attacker == b.Attacker)
        {
            throw new ArgumentException("Each move must have a different attacker.");
        }

        // Tick status effects before moves — fires on the turn after they were applied
        Creature1.ApplyEndOfTurnEffects();
        if (!IsOver)
        {
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
            Creature2.ApplyEndOfTurnEffects();
        }

        var (first, second) = DetermineMoveOrder(a, b);
<<<<<<< HEAD
        ApplyMoveIfPossible(first);
        ApplyMoveIfPossible(second);

        // TODO: Tick CC after both moves resolve

=======

        ApplyMoveIfPossible(first);
        ApplyMoveIfPossible(second);

>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
        TryGetResult(out var outcome);
        return new TurnResult(
            creature1Hp: Creature1.CurrentHp,
            creature2Hp: Creature2.CurrentHp,
            creature1Status: Creature1.Status,
            creature2Status: Creature2.Status,
<<<<<<< HEAD
            finalResult: outcome
        );
    }

    private static Creature GetActor(Move move) => move switch {
        TargetedMove t => t.Source,
        SelfMove s => s.Self,
        _ => throw new ArgumentException($"Unknown move type: {move.GetType().Name}")
    };

    private void ValidateMove(Move move, string paramName) {
        var actor = GetActor(move);

        if (actor != Creature1 && actor != Creature2) {
            throw new ArgumentException("Actor does not belong to this battle.", paramName);
        }

        if (move is TargetedMove targeted) {
            if (targeted.Destination != Creature1 && targeted.Destination != Creature2) {
                throw new ArgumentException("Target does not belong to this battle.", paramName);
            }

            if (targeted.Source == targeted.Destination) {
                throw new ArgumentException("Source and destination must be different creatures.", paramName);
            }
        }
    }

    private (Move first, Move second) DetermineMoveOrder(Move a, Move b) {
        var actorA = GetActor(a);
        var actorB = GetActor(b);

        if (actorA.Initiative > actorB.Initiative) {
            return (a, b);
        }

        if (actorB.Initiative > actorA.Initiative) {
            return (b, a);
        }

        // deterministic tie for consistency (e.g. testing)
        return ReferenceEquals(actorA, Creature1) ? (a, b) : (b, a);
    }

    private void ApplyMoveIfPossible(Move move) {
        if (IsOver || GetActor(move).IsDead) {
            return;
        }

        // TODO: stun skips entire move
        // TODO: silence blocks mana spending moves

        ApplyMove(move);
    }

    private static void ApplyMove(Move move) {
        switch (move) {
            case DamageMove dm:
                dm.Destination.TakeDamage(dm.DamageAmount, dm.Kind);
                break;

            case HealMove hm:
                if (hm.Self.TryConsumeMana(hm.ManaCost)) {
                    hm.Self.Heal(hm.HealAmount);
                }
                break;

            case ShieldMove sm:
                if (sm.Self.TryConsumeMana(sm.ManaCost)) {
                    sm.Self.ApplyShield(sm.ShieldAmount);
                }
                break;

            case ManaRestoreMove mrm:
                mrm.Self.RestoreMana(mrm.ManaAmount);
                break;

            case ManaBurnMove mbm:
                mbm.Self.TryConsumeMana(mbm.ManaAmount);
                break;

            case ManaDrainMove mdm:
                mdm.Destination.TryConsumeMana(mdm.ManaAmount);
                break;

            case ResistanceShredMove rsm:
                if (rsm.Kind == DamageKind.Physical) {
                    rsm.Destination.PhysicalResistance =
                        Math.Max(0, rsm.Destination.PhysicalResistance - rsm.FlatShred);
                }
                else {
                    rsm.Destination.MagicalResistance =
                        Math.Max(0, rsm.Destination.MagicalResistance - rsm.FlatShred);
                }
                break;

            case StatusDamageMove sdm:
                sdm.Destination.TakeDamage(sdm.DamageAmount, sdm.Kind);
                if (!sdm.Destination.IsDead) {
                    sdm.Destination.ApplyStatus(sdm.Effect);
                }
                break;

            case CrowdControlMove ccm:
                // TODO: Implement crowd control effects...
                break;

            default:
                throw new ArgumentException($"Unhandled move type: {move.GetType().Name}");
=======
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

        if (move.InflictedStatus is not null && !move.Target.IsDead)
        {
            move.Target.ApplyStatus(move.InflictedStatus.Value);
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
        }
    }
}
