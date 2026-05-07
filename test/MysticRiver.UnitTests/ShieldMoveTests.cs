using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class ShieldMoveTests {
    private static DamageMove Attack(Creature source, Creature destination, int damage) =>
        new DamageMove(damage, DamageKind.Physical) { Source = source, Destination = destination };

    private static ShieldMove Shield(Creature self, int shieldAmount, int manaCost = 10) =>
        new ShieldMove(shieldAmount, manaCost) { Self = self };

    [Fact]
    public void ShieldMove_AbsorbsDamageTakenSameTurn() {
        var p1 = new Creature("Creature1", 100, 20); // higher initiative, shields first
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(Shield(p1, 15), Attack(p2, p1, 20));

        Assert.Equal(95, p1.CurrentHp);  // 20 damage - 15 shield = 5 through
        Assert.Equal(0, p1.CurrentShield); // shield fully consumed
    }

    [Fact]
    public void ShieldMove_WhenDamageLessThanShield_LeavesResidualShield() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(Shield(p1, 30), Attack(p2, p1, 10));

        Assert.Equal(100, p1.CurrentHp);  // all absorbed
        Assert.Equal(20, p1.CurrentShield); // 10 remaining
    }

    [Fact]
    public void ShieldMove_ConsumesManaCost() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(Shield(p1, 10, manaCost: 40), Attack(p2, p1, 1));

        Assert.Equal(60, p1.CurrentMana);
    }

    [Fact]
    public void ShieldMove_WhenInsufficientMana_DoesNotApplyShield() {
        var p1 = new Creature("Creature1", 100, 20, maxMana: 10);
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(Shield(p1, 50, manaCost: 50), Attack(p2, p1, 20));

        Assert.Equal(80, p1.CurrentHp);   // no shield, full damage taken
        Assert.Equal(0, p1.CurrentShield);
    }

    [Fact]
    public void ShieldMove_Stacks_AcrossMultipleTurns() {
        var p1 = new Creature("Creature1", 100, 20);
        var p2 = new Creature("Creature2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(Shield(p1, 10), Attack(p2, p1, 5)); // 10 shield, 5 absorbed -> 5 left
        battle.ExecuteTurn(Shield(p1, 10), Attack(p2, p1, 5)); // 5+10=15 shield, 5 absorbed -> 10 left

        Assert.Equal(100, p1.CurrentHp);
        Assert.Equal(10, p1.CurrentShield);
    }
}
