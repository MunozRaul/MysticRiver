using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class CreatureTests {
    [Fact]
    public void IsDead_WhenHpAboveZero_ReturnsFalse() {
        var creature = new Creature("Gruk", 100, 10);

        Assert.False(creature.IsDead);
    }

    [Fact]
    public void IsDead_WhenHpReducedToZero_ReturnsTrue() {
        var creature = new Creature("Gruk", 100, 10);

        creature.TakeDamage(100, DamageKind.Physical);

        Assert.True(creature.IsDead);
    }

    [Fact]
    public void IsDead_WhenDamageExceedsHp_ReturnsTrueAndHpIsZero() {
        var creature = new Creature("Gruk", 50, 10);

        creature.TakeDamage(200, DamageKind.Physical);

        Assert.True(creature.IsDead);
        Assert.Equal(0, creature.CurrentHp);
    }

    [Fact]
    public void TakeDamage_ReducesHpByAmount() {
        var creature = new Creature("Gruk", 100, 10);

        creature.TakeDamage(30, DamageKind.Physical);

        Assert.Equal(70, creature.CurrentHp);
    }

    [Fact]
    public void TakeDamage_WhenAmountIsZero_DoesNotReduceHp() {
        // TakeDamage uses ThrowIfNegative -> zero is a valid no-op, not an error
        var creature = new Creature("Gruk", 100, 10);

        creature.TakeDamage(0, DamageKind.Physical);

        Assert.Equal(100, creature.CurrentHp);
    }

    [Fact]
    public void TakeDamage_WhenAmountIsNegative_ThrowsArgumentOutOfRangeException() {
        var creature = new Creature("Gruk", 100, 10);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            creature.TakeDamage(-1, DamageKind.Physical));

        Assert.Equal("amount", ex.ParamName);
    }
}

