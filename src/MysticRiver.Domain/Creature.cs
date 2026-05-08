namespace MysticRiver.Domain;

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
    private int statusTurnsRemaining;

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

        Name = name;
        MaxHp = maxHp;
        CurrentHp = maxHp;
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

        if (actualDamage > 0 && Status == StatusEffect.Sleep)
        {
            ClearStatus();
        }
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
    }

    public void TakeDamage(int amount) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        TakeDamage(amount, DamageKind.Physical);
    }

    public void ApplyStatus(StatusEffect effect)
    {
        Status = effect;
        statusTurnsRemaining = effect switch
        {
            StatusEffect.Paralysis => 2,
            StatusEffect.Sleep     => 2,
            StatusEffect.Freeze    => 1,
            StatusEffect.Silence   => 2,
            _                      => 0
        };
    }

    public void ClearStatus()
    {
        Status = null;
        statusTurnsRemaining = 0;
    }

    internal bool IsSilenced => Status == StatusEffect.Silence;

    /// <summary>
    /// Consumes one turn of the Silence status. Should be called once per turn the creature acts.
    /// </summary>
    internal void TickSilence()
    {
        if (Status != StatusEffect.Silence)
        {
            return;
        }

        statusTurnsRemaining--;
        if (statusTurnsRemaining <= 0)
        {
            ClearStatus();
        }
    }

    /// <summary>
    /// Returns <c>true</c> and consumes one turn of the disabling status when the creature
    /// should skip its action this turn.  Freeze has a 15 % chance of skipping.
    /// </summary>
    internal bool ConsumeStatusSkip(Func<double> roll)
    {
        if (Status is null)
        {
            return false;
        }

        switch (Status.Value)
        {
            case StatusEffect.Paralysis:
            case StatusEffect.Sleep:
                statusTurnsRemaining--;
                if (statusTurnsRemaining <= 0)
                {
                    ClearStatus();
                }
                return true;

            case StatusEffect.Freeze:
                ClearStatus();
                return roll() < 0.15;

            default:
                return false;
        }
    }

    internal void ApplyEndOfTurnEffects()
    {
        if (Status is null) {
            return;
        }

        var damage = Status.Value switch
        {
            StatusEffect.Poison => MaxHp / 8,
            StatusEffect.Burn   => MaxHp / 16,
            StatusEffect.Toxic  => MaxHp / 16,
            _                   => 0
        };
        if (damage > 0)
        {
            TakeDamage(damage, DamageKind.Magical);
        }
    }
}
