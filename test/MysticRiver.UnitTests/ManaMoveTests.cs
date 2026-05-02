using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class ManaMoveTests {
    private static DamageMove Attack(Creature source, Creature destination, int damage) =>
        new DamageMove(damage, DamageKind.Physical) { Source = source, Destination = destination };

    [Fact]
    public void ManaRestoreMove_RestoresMana() {
        var p1 = new Creature("Creature1", 100, 20, maxMana: 100);
        var p2 = new Creature("Creature2", 100, 10);
        p1.TryConsumeMana(60);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ManaRestoreMove(30) { Self = p1 },
            Attack(p2, p1, 1));

        Assert.Equal(70, p1.CurrentMana); // 40 + 30
    }

    [Fact]
    public void ManaRestoreMove_CannotExceedMaxMana() {
        var p1 = new Creature("Creature1", 100, 20, maxMana: 100);
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ManaRestoreMove(999) { Self = p1 },
            Attack(p2, p1, 1));

        Assert.Equal(100, p1.CurrentMana);
    }

    [Fact]
    public void ManaBurnMove_ReducesOwnMana() {
        var p1 = new Creature("Creature1", 100, 20, maxMana: 100);
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ManaBurnMove(40) { Self = p1 },
            Attack(p2, p1, 1));

        Assert.Equal(60, p1.CurrentMana);
    }

    [Fact]
    public void ManaBurnMove_WhenInsufficientMana_DoesNotGoBelowZero() {
        var p1 = new Creature("Creature1", 100, 20, maxMana: 10);
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ManaBurnMove(999) { Self = p1 },
            Attack(p2, p1, 1));

        Assert.Equal(10, p1.CurrentMana); // TryConsumeMana fails silently — mana unchanged
    }

    [Fact]
    public void ManaDrainMove_ReducesTargetMana() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10, maxMana: 100);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ManaDrainMove(40) { Source = p1, Destination = p2 },
            Attack(p2, p1, 1));

        Assert.Equal(60, p2.CurrentMana);
    }

    [Fact]
    public void ManaDrainMove_WhenTargetHasInsufficientMana_DoesNotDrainPartially() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10, maxMana: 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ManaDrainMove(999) { Source = p1, Destination = p2 },
            Attack(p2, p1, 1));

        Assert.Equal(10, p2.CurrentMana); // TryConsumeMana fails silently — mana unchanged
    }
}