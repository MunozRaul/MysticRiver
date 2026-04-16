namespace MysticRiver.Domain;

public sealed class Creature
{
    public string Name { get; }
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }
    public int Initiative { get; }

    public bool IsDead => CurrentHp <= 0;

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
}
