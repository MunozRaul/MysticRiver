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
    public StatusEffect Status => GetStatusFlags();
    public CrowdControlKind CrowdControl { get; private set; }
    public int CrowdControlTurnsRemaining { get; private set; }
    public bool IsCrowdControlled => CrowdControl != CrowdControlKind.None;
    public bool IsStunned => CrowdControl.HasFlag(CrowdControlKind.Stun);
    public bool IsCrowdControlSilenced => CrowdControl.HasFlag(CrowdControlKind.Silence);
    public int EffectiveInitiative => Math.Max(0, Initiative + GetInitiativeModifier());
    private const int maxStatusStacks = 3;
    private readonly Dictionary<StatusEffect, StatusState> _statusStates = new();

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

        if (actualDamage > 0 && HasStatus(StatusEffect.Sleep))
        {
            RemoveStatus(StatusEffect.Sleep);
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
        if (!IsSingleStatus(effect))
        {
            throw new ArgumentOutOfRangeException(nameof(effect), effect, "Status effect must be a single flag value.");
        }

        var defaultDuration = GetDefaultStatusDuration(effect);

        if (!_statusStates.TryGetValue(effect, out var state))
        {
            _statusStates[effect] = new StatusState
            {
                Stacks = 1,
                RemainingTurns = defaultDuration,
            };
            return;
        }

        if (IsStackableStatus(effect))
        {
            state.Stacks = Math.Min(maxStatusStacks, state.Stacks + 1);
        }
        else
        {
            state.Stacks = 1;
        }

        state.RemainingTurns = Math.Max(state.RemainingTurns, defaultDuration);
    }

    public void ClearStatus()
    {
        _statusStates.Clear();
    }

    public bool HasStatus(StatusEffect effect) => _statusStates.ContainsKey(effect);

    public int GetStatusStacks(StatusEffect effect) =>
        _statusStates.TryGetValue(effect, out var state) ? state.Stacks : 0;

    public int GetStatusTurnsRemaining(StatusEffect effect) =>
        _statusStates.TryGetValue(effect, out var state) ? state.RemainingTurns : 0;

    /// <summary>
    /// Returns <c>true</c> and consumes one turn of the disabling status when the creature
    /// should skip its action this turn.  Freeze has a 15 % chance of skipping.
    /// </summary>
    internal bool ConsumeStatusSkip(Func<double> roll)
    {
        if (_statusStates.Count == 0)
        {
            return false;
        }

        var skipped = false;

        if (TryConsumeSkipStatus(StatusEffect.Paralysis))
        {
            skipped = true;
        }

        if (TryConsumeSkipStatus(StatusEffect.Sleep))
        {
            skipped = true;
        }

        if (TryConsumeFreeze(roll))
        {
            skipped = true;
        }

        return skipped;
    }

    internal void ApplyEndOfTurnEffects()
    {
        if (_statusStates.Count == 0) {
            return;
        }

        var totalDamage = 0;
        var toRemove = new List<StatusEffect>();

        var snapshot = new List<KeyValuePair<StatusEffect, StatusState>>(_statusStates);
        foreach (var entry in snapshot)
        {
            var effect = entry.Key;
            var state = entry.Value;

            if (IsDamageOverTimeStatus(effect))
            {
                totalDamage += GetStatusDamagePerStack(effect) * Math.Max(1, state.Stacks);
                state.RemainingTurns--;
                if (state.RemainingTurns <= 0)
                {
                    toRemove.Add(effect);
                }
                continue;
            }

            if (IsInitiativeStatus(effect))
            {
                state.RemainingTurns--;
                if (state.RemainingTurns <= 0)
                {
                    toRemove.Add(effect);
                }
            }
        }

        if (totalDamage > 0)
        {
            TakeDamage(totalDamage, DamageKind.Magical);
        }

        foreach (var effect in toRemove)
        {
            RemoveStatus(effect);
        }
    }

    /// <summary>
    /// Applies crowd control effect for a number of turns.
    /// Reapplying the same effect refreshes the duration to the longer of the two; different effects overwrite.
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

    private bool TryConsumeSkipStatus(StatusEffect effect)
    {
        if (!_statusStates.TryGetValue(effect, out var state))
        {
            return false;
        }

        state.RemainingTurns--;
        if (state.RemainingTurns <= 0)
        {
            RemoveStatus(effect);
        }

        return true;
    }

    private bool TryConsumeFreeze(Func<double> roll)
    {
        if (!_statusStates.TryGetValue(StatusEffect.Freeze, out var state))
        {
            return false;
        }

        var skipped = roll() < 0.15;
        state.RemainingTurns--;
        if (state.RemainingTurns <= 0)
        {
            RemoveStatus(StatusEffect.Freeze);
        }

        return skipped;
    }

    private void RemoveStatus(StatusEffect effect)
    {
        _statusStates.Remove(effect);
    }

    private StatusEffect GetStatusFlags()
    {
        var flags = StatusEffect.None;
        foreach (var effect in _statusStates.Keys)
        {
            flags |= effect;
        }

        return flags;
    }

    private static bool IsSingleStatus(StatusEffect effect)
    {
        if (effect == StatusEffect.None)
        {
            return false;
        }

        var value = (int)effect;
        return (value & (value - 1)) == 0;
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
        var modifier = 0;

        if (_statusStates.TryGetValue(StatusEffect.Haste, out var haste))
        {
            modifier += 5 * Math.Max(1, haste.Stacks);
        }

        if (_statusStates.TryGetValue(StatusEffect.Slow, out var slow))
        {
            modifier -= 5 * Math.Max(1, slow.Stacks);
        }

        return modifier;
    }

    private void ClearCrowdControl() {
        CrowdControl = CrowdControlKind.None;
        CrowdControlTurnsRemaining = 0;
    }

    private sealed class StatusState
    {
        public int Stacks { get; set; }
        public int RemainingTurns { get; set; }
    }
}
