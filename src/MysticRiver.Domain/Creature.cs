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
    public StatusEffect Status { get; private set; }
    public int StatusTurnsRemaining { get; private set; }
    public int StatusStacks { get; private set; }
    public CrowdControlKind CrowdControl { get; private set; }
    public int CrowdControlTurnsRemaining { get; private set; }
    public bool IsCrowdControlled => CrowdControl != CrowdControlKind.None;
    public bool IsStunned => CrowdControl.HasFlag(CrowdControlKind.Stun);
    public bool IsCrowdControlSilenced => CrowdControl.HasFlag(CrowdControlKind.Silence);
    public int EffectiveInitiative => Math.Max(0, Initiative + GetInitiativeModifier());
    private const int maxStatusStacks = 3;

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

        if (actualDamage > 0 && Status.HasFlag(StatusEffect.Sleep))
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
        var wasSameStatus = Status == effect;

        Status = effect;
        StatusTurnsRemaining = GetDefaultStatusDuration(effect);

        if (IsStackableStatus(effect))
        {
            StatusStacks = wasSameStatus
                ? Math.Min(maxStatusStacks, StatusStacks + 1)
                : 1;
        }
        else
        {
            StatusStacks = 1;
        }
    }

    public void ClearStatus()
    {
        Status = StatusEffect.None;
        StatusTurnsRemaining = 0;
        StatusStacks = 0;
    }

    /// <summary>
    /// Returns <c>true</c> and consumes one turn of the disabling status when the creature
    /// should skip its action this turn.  Freeze has a 15 % chance of skipping.
    /// </summary>
    internal bool ConsumeStatusSkip(Func<double> roll)
    {
        if (Status == StatusEffect.None)
        {
            return false;
        }

        if (Status.HasFlag(StatusEffect.Paralysis) || Status.HasFlag(StatusEffect.Sleep))
        {
            StatusTurnsRemaining--;
            if (StatusTurnsRemaining <= 0)
            {
                ClearStatus();
            }
            return true;
        }

        if (Status.HasFlag(StatusEffect.Freeze))
        {
            StatusTurnsRemaining--;
            if (StatusTurnsRemaining <= 0)
            {
                ClearStatus();
            }
            return roll() < 0.15;
        }

        return false;
    }

    internal void ApplyEndOfTurnEffects()
    {
        if (Status == StatusEffect.None) {
            return;
        }

        if (IsDamageOverTimeStatus(Status))
        {
            var damage = GetStatusDamagePerStack(Status) * Math.Max(1, StatusStacks);
            if (damage > 0)
            {
                TakeDamage(damage, DamageKind.Magical);
            }

            StatusTurnsRemaining--;
            if (StatusTurnsRemaining <= 0)
            {
                ClearStatus();
            }
            return;
        }

        if (IsInitiativeStatus(Status))
        {
            StatusTurnsRemaining--;
            if (StatusTurnsRemaining <= 0)
            {
                ClearStatus();
            }
        }
    }

    /// <summary>
    /// Applies crowd control effect for a number of turns. 
    /// If the creature is already crowd controlled, the new effect and duration overwrite the old ones.
    /// </summary>
    /// <param name="cc"></param>
    /// <param name="turns"></param>
    public void ApplyCrowdControl(CrowdControlKind cc, int turns) {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(turns);

        if (CrowdControl == cc) {
            CrowdControlTurnsRemaining = Math.Max(CrowdControlTurnsRemaining, turns);
            return;
        }

        CrowdControl = cc;
        CrowdControlTurnsRemaining = turns;
    }

    /// <summary>
    /// Decrements the crowd control duration by one. Called once per turn regardless of whether the creature acted.
    /// Clears the effect when the duration reaches zero.
    /// </summary>
    internal void TickCrowdControl() {
        if (!IsCrowdControlled) {
            return;
        }

        CrowdControlTurnsRemaining--;
        if (CrowdControlTurnsRemaining <= 0) {
            ClearCrowdControl();
        }
    }

    private static int GetDefaultStatusDuration(StatusEffect effect)
    {
        return effect switch
        {
            StatusEffect.Paralysis => 2,
            StatusEffect.Sleep => 2,
            StatusEffect.Freeze => 1,
            StatusEffect.Poison => 4,
            StatusEffect.Burn => 3,
            StatusEffect.Toxic => 4,
            StatusEffect.Bleed => 3,
            StatusEffect.Haste => 3,
            StatusEffect.Slow => 3,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown status effect."),
        };
    }

    private static bool IsStackableStatus(StatusEffect effect)
    {
        return effect is StatusEffect.Poison or StatusEffect.Burn or StatusEffect.Toxic or StatusEffect.Bleed;
    }

    private static bool IsDamageOverTimeStatus(StatusEffect effect)
    {
        return effect is StatusEffect.Poison or StatusEffect.Burn or StatusEffect.Toxic or StatusEffect.Bleed;
    }

    private static bool IsInitiativeStatus(StatusEffect effect)
    {
        return effect is StatusEffect.Haste or StatusEffect.Slow;
    }

    private int GetStatusDamagePerStack(StatusEffect effect)
    {
        return effect switch
        {
            StatusEffect.Poison => MaxHp / 8,
            StatusEffect.Burn => MaxHp / 16,
            StatusEffect.Toxic => MaxHp / 16,
            StatusEffect.Bleed => MaxHp / 12,
            _ => 0,
        };
    }

    private int GetInitiativeModifier()
    {
        if (Status == StatusEffect.None)
        {
            return 0;
        }

        var stacks = Math.Max(1, StatusStacks);

        return Status switch
        {
            StatusEffect.Haste => 5 * stacks,
            StatusEffect.Slow => -5 * stacks,
            _ => 0,
        };
    }

    private void ClearCrowdControl() {
        CrowdControl = CrowdControlKind.None;
        CrowdControlTurnsRemaining = 0;
    }
}
