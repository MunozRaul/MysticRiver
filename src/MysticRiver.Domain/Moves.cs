namespace MysticRiver.Domain;

public enum DamageKind {
    Physical,
    Magical,
}

public enum CrowdControlKind {
    Silence,
    Stun,
}

// marker so we can pattern match on targeted and self move later and treat it like a union
public abstract record Move;

public abstract record TargetedMove : Move {
    public required Creature Source { get; init; }
    public required Creature Destination { get; init; }
}

public abstract record SelfMove : Move {
    public required Creature Self { get; init; }
}

public sealed record HealMove(int HealAmount, int ManaCost) : SelfMove;
public sealed record ManaBurnMove(int ManaAmount) : SelfMove;
public sealed record ShieldMove(int ShieldAmount, int ManaCost) : SelfMove;
public sealed record ManaRestoreMove(int ManaAmount) : SelfMove;

public sealed record DamageMove(int DamageAmount, DamageKind Kind) : TargetedMove;
public sealed record ManaDrainMove(int ManaAmount) : TargetedMove;
public sealed record ResistanceShredMove(int FlatShred, DamageKind Kind) : TargetedMove;
public sealed record CrowdControlMove(int Turns, CrowdControlKind CrowdControlType) : TargetedMove;

/*
    Maybe we also want...
    - Lifesteal
    - More Crowd Control (Silence, Stun, ...)
    - Any Counter Moves?
*/
