namespace MysticRiver.Domain;

public sealed record Move 
{
    public Creature Attacker { get; }
    public Creature Target { get; }
    public MoveType Type { get; }
    public int Power { get; }
    public StatusEffect InflictedStatus { get; }

    private Move(Creature attacker, Creature target, MoveType type, int power, StatusEffect inflictedStatus = StatusEffect.None)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(power);

        if (attacker == target)
        {
            throw new ArgumentException("Attacker and target must be different creatures.", nameof(target));
        }

        Attacker = attacker;
        Target = target;
        Type = type;
        Power = power;
        InflictedStatus = inflictedStatus;
    }

    /// <summary>
    /// Creates a basic attack move.
    /// </summary>
    public static Move BasicAttack(Creature attacker, Creature target, int power)
        => new(attacker, target, MoveType.BasicAttack, power);

    /// <summary>
    /// Creates a status attack move that deals damage and inflicts a status effect on the target.
    /// </summary>
    public static Move StatusAttack(Creature attacker, Creature target, int power, StatusEffect status)
        => new(attacker, target, MoveType.StatusAttack, power, status);

    /// <summary>
    /// Resolves this move to a damage value.
    /// </summary>
    public int ResolveDamage()
        => Type switch
        {
            MoveType.BasicAttack => Power,
            MoveType.StatusAttack => Power,
            _ => throw new InvalidOperationException($"Unsupported move type: {Type}")
        };
}