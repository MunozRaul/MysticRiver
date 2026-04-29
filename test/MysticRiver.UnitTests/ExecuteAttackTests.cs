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
    public void ExecuteTurn_WithReversedArguments_RemainsDeterministic() {
        var (battle1, p1a, p2a) = CreateBattle(100, 100);
        var (battle2, p1b, p2b) = CreateBattle(100, 100);

        var resultA = battle1.ExecuteTurn(Attack(p1a, p2a, 20), Attack(p2a, p1a, 15));
        var resultB = battle2.ExecuteTurn(Attack(p2b, p1b, 15), Attack(p1b, p2b, 20)); // reversed

        Assert.Equal(resultA.Creature1Hp, resultB.Creature1Hp);
        Assert.Equal(resultA.Creature2Hp, resultB.Creature2Hp);
    }

    [Fact]
    public void ExecuteTurn_WhenFirstAttackEndsBattle_SecondAttackIsSkipped() {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 20, initiative1: 20, initiative2: 10);

        // p1 has higher initiative and deals exactly 20 damage — enough to kill p2 in the first move
        var result = battle.ExecuteTurn(
            Attack(p1, p2, 20),
            Attack(p2, p1, 15));

        Assert.Equal(100, p1.CurrentHp); // p2's counter-attack was skipped
        Assert.Equal(0, p2.CurrentHp);
        Assert.True(result.BattleEnded);
        Assert.NotNull(result.FinalResult);
        Assert.Equal(p1, result.FinalResult.Winner);
        Assert.Equal(p2, result.FinalResult.Loser);
    }

    [Fact]
    public void ExecuteTurn_WithNullMove_ThrowsArgumentNullException() {
        var (battle, p1, p2) = CreateBattle();
        var validMove = Attack(p1, p2, 10);

        Assert.Throws<ArgumentNullException>(() => battle.ExecuteTurn(null!, validMove));
        Assert.Throws<ArgumentNullException>(() => battle.ExecuteTurn(validMove, null!));
    }

    [Fact]
    public void ExecuteTurn_WithSameAttacker_ThrowsArgumentException() {
        var (battle, p1, p2) = CreateBattle();

        Assert.Throws<ArgumentException>(() => battle.ExecuteTurn(
            Attack(p1, p2, 10),
            Attack(p1, p2, 15)));
    }

    [Fact]
    public void ExecuteTurn_WithAttackerNotInBattle_ThrowsArgumentException() {
        var (battle, p1, p2) = CreateBattle();
        var outsider = new Creature("Outsider", 100, 10);

        Assert.Throws<ArgumentException>(() => battle.ExecuteTurn(
            Attack(p1, p2, 10),
            Attack(outsider, p1, 15)));
    }

    [Fact]
    public void ExecuteTurn_WhenBattleIsOver_ThrowsInvalidOperationException() {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 10, initiative1: 20, initiative2: 10);

        // Kill p2 in the first turn
        battle.ExecuteTurn(Attack(p1, p2, 10), Attack(p2, p1, 5));

        Assert.Throws<InvalidOperationException>(() => battle.ExecuteTurn(
            Attack(p1, p2, 1),
            Attack(p2, p1, 1)));
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
