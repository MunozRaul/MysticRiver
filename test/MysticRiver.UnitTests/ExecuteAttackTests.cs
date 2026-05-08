// test/MysticRiver.UnitTests/ExecuteAttackTests.cs
using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class ExecuteAttackTests
{
    private static (Battle battle, Creature p1, Creature p2) CreateBattle(
        int hp1 = 100,
        int hp2 = 100,
        int initiative1 = 10,
        int initiative2 = 10)
    {
        var p1 = new Creature("Creature1", hp1, initiative1);
        var p2 = new Creature("Creature2", hp2, initiative2);
        return (new Battle(p1, p2), p1, p2);
    }

    private static DamageMove Attack(Creature source, Creature destination, int damage) =>
        new(damage, DamageKind.Physical) { Source = source, Destination = destination };

    [Fact]
    public void Attack_Should_Reduce_Both_Hp()
    {
        var (battle, p1, p2) = CreateBattle(100, 100);

        var creature1Attack = Attack(p1, p2, 20);
        var creature2Attack = Attack(p2, p1, 15);

        var result = battle.ExecuteTurn(creature1Attack, creature2Attack);

        Assert.Equal(85, p1.CurrentHp);
        Assert.Equal(80, p2.CurrentHp);
        Assert.Equal(85, result.Creature1Hp);
        Assert.Equal(80, result.Creature2Hp);
        Assert.False(result.BattleEnded);
        Assert.Null(result.FinalResult);
    }

    [Fact]
    public void Attack_Should_Be_Deterministic()
    {
        var (battleA, p1A, p2A) = CreateBattle(100, 100);
        var (battleB, p1B, p2B) = CreateBattle(100, 100);

        var creature1AttackA = Attack(p1A, p2A, 20);
        var creature2AttackA = Attack(p2A, p1A, 15);
        var creature1AttackB = Attack(p1B, p2B, 20);
        var creature2AttackB = Attack(p2B, p1B, 15);

        var resultA = battleA.ExecuteTurn(creature1AttackA, creature2AttackA);
        var resultB = battleB.ExecuteTurn(creature1AttackB, creature2AttackB);

        Assert.Equal(resultA.Creature1Hp, resultB.Creature1Hp);
        Assert.Equal(resultA.Creature2Hp, resultB.Creature2Hp);
        Assert.Equal(p1A.CurrentHp, p1B.CurrentHp);
        Assert.Equal(p2A.CurrentHp, p2B.CurrentHp);
    }

    [Fact]
    public void ExecuteTurn_WithReversedArguments_RemainsDeterministic()
    {
        var (battle1, p1a, p2a) = CreateBattle(100, 100);
        var (battle2, p1b, p2b) = CreateBattle(100, 100);

        var creature1AttackA = Attack(p1a, p2a, 20);
        var creature2AttackA = Attack(p2a, p1a, 15);
        var creature1AttackB = Attack(p1b, p2b, 20);
        var creature2AttackB = Attack(p2b, p1b, 15);

        var resultA = battle1.ExecuteTurn(creature1AttackA, creature2AttackA);
        var resultB = battle2.ExecuteTurn(creature2AttackB, creature1AttackB); // Reversed order

        Assert.Equal(resultA.Creature1Hp, resultB.Creature1Hp);
        Assert.Equal(resultA.Creature2Hp, resultB.Creature2Hp);
    }

    [Fact]
    public void ExecuteTurn_WhenFirstAttackEndsBattle_SecondAttackIsSkipped()
    {
        var (battle, p1, p2) = CreateBattle(100, 20);

        var creature1Attack = Attack(p1, p2, 20);
        var creature2Attack = Attack(p2, p1, 15);

        var result = battle.ExecuteTurn(creature1Attack, creature2Attack);

        Assert.Equal(100, p1.CurrentHp);
        Assert.Equal(0, p2.CurrentHp);
        Assert.True(result.BattleEnded);
        Assert.NotNull(result.FinalResult);
        Assert.Equal(p1, result.FinalResult.Winner);
        Assert.Equal(p2, result.FinalResult.Loser);
    }

    [Fact]
    public void ExecuteTurn_WithNullMove_ThrowsArgumentNullException()
    {
        var (battle, p1, p2) = CreateBattle();
        var validMove = Attack(p1, p2, 10);

        Assert.Throws<ArgumentNullException>(() => battle.ExecuteTurn(null!, validMove));
        Assert.Throws<ArgumentNullException>(() => battle.ExecuteTurn(validMove, null!));
    }

    [Fact]
    public void ExecuteTurn_WithSameAttacker_ThrowsArgumentException()
    {
        var (battle, p1, p2) = CreateBattle();

        var firstMove = Attack(p1, p2, 10);
        var secondMove = Attack(p1, p2, 15);

        Assert.Throws<ArgumentException>(() => battle.ExecuteTurn(firstMove, secondMove));
    }

    [Fact]
    public void ExecuteTurn_WithAttackerNotInBattle_ThrowsArgumentException()
    {
        var (battle, p1, p2) = CreateBattle();
        var outsider = new Creature("Outsider", 100, 10);

        var moveA = Attack(p1, p2, 10);
        var moveB = Attack(outsider, p1, 15);

        Assert.Throws<ArgumentException>(() => battle.ExecuteTurn(moveA, moveB));    
    }

    [Fact]
    public void ExecuteTurn_WhenBattleIsOver_ThrowsInvalidOperationException()
    {
        var (battle, p1, p2) = CreateBattle(100, 10);

        battle.ExecuteTurn(Attack(p1, p2, 10), Attack(p2, p1, 1));

        var move1 = Attack(p1, p2, 1);
        var move2 = Attack(p2, p1, 1);

        Assert.Throws<InvalidOperationException>(() => battle.ExecuteTurn(move1, move2));
    }

    [Fact]
    public void ExecuteTurn_WithNegativeDamage_ThrowsArgumentOutOfRangeException()
    {
        var (battle, p1, p2) = CreateBattle();

        Assert.Throws<ArgumentOutOfRangeException>(() => battle.ExecuteTurn(Attack(p1, p2, -1), Attack(p2, p1, 1)));
    }
}