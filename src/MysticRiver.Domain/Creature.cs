namespace MysticRiver.Domain;

public sealed class Creature
{
    public string Name { get; }
    public int MaxHp { get; }
    public int CurrentHp { get; private set; }

    public bool IsDead => CurrentHp <= 0;

    public Creature(string name, int maxHp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHp);

        Name = name;
        MaxHp = maxHp;
        CurrentHp = maxHp;
    }

    public void TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        CurrentHp = Math.Max(0, CurrentHp - amount);
    }
}
