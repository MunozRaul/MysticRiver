using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class LifestealMoveTests
{
    [Fact]
    public void Lifesteal_HealsForPortionOfDamageDealt()
    {
        var p1 = new Creature("p1", 100, 10);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        p1.TakeDamage(50);

        battle.ExecuteTurn(
            new LifestealMove(20, DamageKind.Physical, 0.5) { Source = p1, Destination = p2 },
            new DamageMove(0, DamageKind.Physical) { Source = p2, Destination = p1 });

        Assert.Equal(60, p1.CurrentHp);
        Assert.Equal(80, p2.CurrentHp);
    }
}
