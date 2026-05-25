namespace MysticRiver.Contracts.Battle;

[Flags]
public enum AbilityTag
{
    Damage = 1,
    Heal = 2,
    Shield = 4,
    Status = 8,
    CrowdControl = 16,
    Lifesteal = 32,
}
