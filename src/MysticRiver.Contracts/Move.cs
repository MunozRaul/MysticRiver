namespace MysticRiver.Contracts;

// dummy interface for now since we dont have any players as of now
public interface IPlayer
{
    // could have things like...
    // public UInt32 HealthPoints { get; set; }
    // public UInt32 Shield { get; set; }

    // public UInt32 MagicDamage { get; set; }
    // public UInt32 MagicResistance { get; set; }
    // public UInt32 MagicResistancePenetrationPercent { get; set; }

    // public UInt32 PhysicalDamage { get; set; }
    // public UInt32 PhysicalResistance { get; set; }
    // public UInt32 PhysicalResistancePenetrationPercent { get; set; }

    // public UInt32 Mana { get; set; }
}
public enum DamageType
{
    Physical,
    Magical,
}

public enum CrowdControlType
{
    Silence,
    Stun,
}

// TODO: this is more of a Command Pattern now, should we instead name it Command rather than Move?
public abstract record Move; // marker so we can pattern match on targeted and self move later and treat it like a union

public abstract record TargetedMove : Move
{
    public required IPlayer Source { get; init; }
    public required IPlayer Destination { get; init; }
}

public abstract record SelfMove : Move
{
    public required IPlayer Self { get; init; }
}

public sealed record HealMove(UInt32 HealAmount, UInt32 ManaCost) : SelfMove;
public sealed record ManaBurnMove(UInt32 ManaAmount) : SelfMove;
public sealed record ShieldMove(UInt32 ShieldAmount, UInt32 ManaCost) : SelfMove;
public sealed record ManaRestoreMove(UInt32 ManaAmount) : SelfMove;

public sealed record DamageMove(UInt32 DamageAmount, DamageType DamageType) : TargetedMove;
public sealed record ManaDrainMove(UInt32 ManaAmount) : TargetedMove;
public sealed record ResistanceShredMove(UInt32 ShredPercent, DamageType DamageType) : TargetedMove;
public sealed record CrowdControlMove(UInt32 Turns, CrowdControlType CrowdControlType) : TargetedMove;

/*
    Maybe we also want...
    - Lifesteal
    - More Crowd Control (Silence, Stun, ...)
    - Any Counter Moves?
*/
