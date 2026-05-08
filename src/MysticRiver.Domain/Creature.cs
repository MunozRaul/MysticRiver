namespace MysticRiver.Domain;

<<<<<<< HEAD
public sealed class Creature {
    public string Name { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; private set; }
    public int MaxMana { get; set; }
    public int CurrentMana { get; private set; }
    public int Initiative { get; set; }
    public int PhysicalResistance { get; set; }
    public int MagicalResistance { get; set; }
    public bool IsDead => CurrentHp <= 0;
    public int CurrentShield { get; private set; }
    public StatusEffect? Status { get; private set; }
    // TODO: Crowd Control

    public Creature(
        string name,
        int maxHp,
        int initiative,
        int maxMana = 100,
        int physicalResistance = 0,
        int magicalResistance = 0) {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHp);
        ArgumentOutOfRangeException.ThrowIfNegative(initiative);
        ArgumentOutOfRangeException.ThrowIfNegative(maxMana);
        ArgumentOutOfRangeException.ThrowIfNegative(physicalResistance);
        ArgumentOutOfRangeException.ThrowIfNegative(magicalResistance);
=======
public sealed class Creature
{
    public string Name { get; }
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public int Initiative { get; }

    public bool IsDead => CurrentHp <= 0;

    public StatusEffect? Status { get; private set; }

    public Creature(string name, int maxHp, int initiative)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHp);
        ArgumentOutOfRangeException.ThrowIfNegative(initiative);
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e

        Name = name;
        MaxHp = maxHp;
        CurrentHp = maxHp;
<<<<<<< HEAD
        MaxMana = maxMana;
        CurrentMana = maxMana;
        Initiative = initiative;
        PhysicalResistance = physicalResistance;
        MagicalResistance = magicalResistance;
    }

    public void ApplyShield(int amount) {
        CurrentShield += Math.Max(0, amount);
    }

    public void TakeDamage(int amount, DamageKind damageKind) {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        var resistance = damageKind == DamageKind.Physical
            ? PhysicalResistance
            : MagicalResistance;

        var actualDamage = Math.Max(0, amount - resistance);

        // Shield absorbs first
        var shieldAbsorb = Math.Min(CurrentShield, actualDamage);
        CurrentShield -= shieldAbsorb;
        actualDamage -= shieldAbsorb;

        CurrentHp = Math.Max(0, CurrentHp - actualDamage);
    }

    public void Heal(int amount) {
        var heal = Math.Max(0, amount);
        CurrentHp = Math.Min(MaxHp, CurrentHp + heal);
    }

    public bool TryConsumeMana(int amount) {
        if (CurrentMana >= amount) {
            CurrentMana -= amount;
            return true;
        }

        return false;
    }

    public void RestoreMana(int amount) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        CurrentMana = Math.Min(MaxMana, CurrentMana + amount);
=======
        Initiative = initiative;
    }

    public void TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        CurrentHp = Math.Max(0, CurrentHp - amount);
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
    }

    public void ApplyStatus(StatusEffect effect) => Status = effect;

    public void ClearStatus() => Status = null;

    internal void ApplyEndOfTurnEffects()
    {
<<<<<<< HEAD
        if (Status is null) {
            return;
        }

        var damage = Status.Value switch
=======
        if (Status is null)
        {
            return;
        }

        var damage = Status switch
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
        {
            StatusEffect.Poison => MaxHp / 8,
            StatusEffect.Burn   => MaxHp / 16,
            StatusEffect.Toxic  => MaxHp / 16,
            _                   => 0
        };
        if (damage > 0)
        {
<<<<<<< HEAD
            TakeDamage(damage, DamageKind.Magical);
=======
            TakeDamage(damage);
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
        }
    }
}
