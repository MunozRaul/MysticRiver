namespace MysticRiver.Domain;

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

        Name = name;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Initiative = initiative;
    }

    public void TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(amount);
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }

    public void ApplyStatus(StatusEffect effect) => Status = effect;

    public void ClearStatus() => Status = null;

    internal void TickStatus()
    {
        if (Status is null)
        {
            return;
        }

        var damage = Status switch
        {
            StatusEffect.Poison => MaxHp / 8,
            StatusEffect.Burn   => MaxHp / 16,
            StatusEffect.Toxic  => MaxHp / 16,
            _                   => 0
        };
        if (damage > 0)
        {
            TakeDamage(damage);
        }
    }
}
