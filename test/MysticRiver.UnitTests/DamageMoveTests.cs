using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class ExecuteAttackTests {
    private static (Battle battle, Creature p1, Creature p2) CreateBattle(
        int hp1 = 100,
        int hp2 = 100,
        int initiative1 = 10,
        int initiative2 = 10) {
        var p1 = new Creature("Creature1", hp1, initiative1);
        var p2 = new Creature("Creature2", hp2, initiative2);
        return (new Battle(p1, p2), p1, p2);
    }

    private static DamageMove Attack(Creature source, Creature destination, int damage) =>
        new DamageMove(damage, DamageKind.Physical) { Source = source, Destination = destination };

    [Fact]
    public void Attack_Should_Reduce_Both_Hp() {
        var (battle, p1, p2) = CreateBattle(100, 100);

        var result = battle.ExecuteTurn(
            Attack(p1, p2, 20),
            Attack(p2, p1, 15)
        );

        Assert.Equal(85, p1.CurrentHp);
        Assert.Equal(80, p2.CurrentHp);
        Assert.Equal(85, result.Creature1Hp);
        Assert.Equal(80, result.Creature2Hp);
        Assert.False(result.BattleEnded);
        Assert.Null(result.FinalResult);
    }

    [Fact]
    public void Attack_Should_Be_Deterministic() {
        var (battleA, p1A, p2A) = CreateBattle(100, 100);
        var (battleB, p1B, p2B) = CreateBattle(100, 100);

        var resultA = battleA.ExecuteTurn(Attack(p1A, p2A, 20), Attack(p2A, p1A, 15));
        var resultB = battleB.ExecuteTurn(Attack(p1B, p2B, 20), Attack(p2B, p1B, 15));

        Assert.Equal(resultA.Creature1Hp, resultB.Creature1Hp);
        Assert.Equal(resultA.Creature2Hp, resultB.Creature2Hp);
        Assert.Equal(p1A.CurrentHp, p1B.CurrentHp);
        Assert.Equal(p2A.CurrentHp, p2B.CurrentHp);
    }

    [Fact]
    public void DamageMove_WithZeroDamage_DealsNoDamageAndDoesNotThrow() {
        var (battle, p1, p2) = CreateBattle(100, 100);

        var result = battle.ExecuteTurn(
            new DamageMove(0, DamageKind.Physical) { Source = p1, Destination = p2 },
            Attack(p2, p1, 10));

        Assert.Equal(90, p1.CurrentHp); // p1 took 10
        Assert.Equal(100, p2.CurrentHp); // p2 took 0, no throw
    }
}
