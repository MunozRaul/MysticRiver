namespace MysticRiver.Domain;

public enum MoveType
{
    BasicAttack = 0
}

public sealed record Move 
{
    public Creature Attacker { get; }
    public Creature Target { get; }
    public MoveType Type { get; }
    public int Power { get; }

    private Move(Creature attacker, Creature target, MoveType type, int power)
    {
        ArgumentNullException.ThrowIfNull(attacker);
        ArgumentNullException.ThrowIfNull(target);
        ArgumentOutOfRangeException.ThrowIfNegative(power);

        if (attacker == target)
        {
            throw new ArgumentException("Attacker and target must be different creatures.", nameof(target));
        }

        Attacker = attacker;
        Target = target;
        Type = type;
        Power = power;
    }

    /// <summary>
    /// Creates a basic attack move.
    /// </summary>
    public static Move BasicAttack(Creature attacker, Creature target, int power)
        =>new(attacker, target, MoveType.BasicAttack, power);

    /// <summary>
    /// Resolves this move to a damage value.
    /// </summary>
    public int ResolveDamage()
        => Type switch
        {
            MoveType.BasicAttack => Power,
            _ => throw new InvalidOperationException($"Unsupported move type: {Type}")
        };
}