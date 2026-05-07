using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class ResistanceShredMoveTests {
    private static DamageMove Attack(Creature source, Creature destination, int damage, DamageKind kind = DamageKind.Physical) =>
        new DamageMove(damage, kind) { Source = source, Destination = destination };

    [Fact]
    public void ResistanceShredMove_ReducesPhysicalResistance() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10, physicalResistance: 20);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ResistanceShredMove(10, DamageKind.Physical) { Source = p1, Destination = p2 },
            Attack(p2, p1, 1));

        Assert.Equal(10, p2.PhysicalResistance);
    }

    [Fact]
    public void ResistanceShredMove_ReducesMagicalResistance() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10, magicalResistance: 30);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ResistanceShredMove(15, DamageKind.Magical) { Source = p1, Destination = p2 },
            Attack(p2, p1, 1));

        Assert.Equal(15, p2.MagicalResistance);
    }

    [Fact]
    public void ResistanceShredMove_CannotReduceResistanceBelowZero() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10, physicalResistance: 5);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            new ResistanceShredMove(999, DamageKind.Physical) { Source = p1, Destination = p2 },
            Attack(p2, p1, 1));

        Assert.Equal(0, p2.PhysicalResistance);
    }

    [Fact]
    public void ResistanceShredMove_IncreasesEffectivePhysicalDamageNextTurn() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10, physicalResistance: 10);
        var battle = new Battle(p1, p2);

        // Turn 1: p1 shreds p2's resistance, p2 retaliates for 1 (irrelevant)
        battle.ExecuteTurn(
            new ResistanceShredMove(10, DamageKind.Physical) { Source = p1, Destination = p2 },
            Attack(p2, p1, 1));

        // Turn 2: p2 now has 0 physical resistance — full 20 damage lands
        battle.ExecuteTurn(
            Attack(p1, p2, 20),
            Attack(p2, p1, 1));

        Assert.Equal(80, p2.CurrentHp);
    }
}
