namespace MysticRiver.Domain;

[Flags]
public enum StatusEffect
{
    None      = 0,
    Poison    = 1,
    Burn      = 2,
    Paralysis = 4,
    Sleep     = 8,
    Freeze    = 16,
    Toxic     = 32,
    Bleed     = 64,
    Haste     = 128,
    Slow      = 256
}
