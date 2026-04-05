// test/MysticRiver.UnitTests/ExecuteAttackTests.cs
using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class ExecuteAttackTests
{
    private static (Battle battle, Creature p1, Creature p2) CreateBattle(int hp1 = 100, int hp2 = 100)
    {
        var p1 = new Creature("Player1", hp1);
        var p2 = new Creature("Player2", hp2);
        return (new Battle(p1, p2), p1, p2);
    }

    [Fact]
    public void Attack_Should_Reduce_Both_Hp()
    {
        var (battle, p1, p2) = CreateBattle(100, 100);

        var player1Attack = Move.BasicAttack(p1, p2, 20);
        var player2Attack = Move.BasicAttack(p2, p1, 15);

        var result = battle.ExecuteTurn(player1Attack, player2Attack);

        Assert.Equal(85, p1.CurrentHp);
        Assert.Equal(80, p2.CurrentHp);
        Assert.Equal(85, result.Player1Hp);
        Assert.Equal(80, result.Player2Hp);
        Assert.False(result.BattleEnded);
        Assert.Null(result.FinalResult);
    }

    [Fact]
    public void Attack_Should_Be_Deterministic()
    {
        var (battleA, p1A, p2A) = CreateBattle(100, 100);
        var (battleB, p1B, p2B) = CreateBattle(100, 100);

        var player1AttackA = Move.BasicAttack(p1A, p2A, 20);
        var player2AttackA = Move.BasicAttack(p2A, p1A, 15);
        var player1AttackB = Move.BasicAttack(p1B, p2B, 20);
        var player2AttackB = Move.BasicAttack(p2B, p1B, 15);

        var resultA = battleA.ExecuteTurn(player1AttackA, player2AttackA);
        var resultB = battleB.ExecuteTurn(player1AttackB, player2AttackB);

        Assert.Equal(resultA.Player1Hp, resultB.Player1Hp);
        Assert.Equal(resultA.Player2Hp, resultB.Player2Hp);
        Assert.Equal(p1A.CurrentHp, p1B.CurrentHp);
        Assert.Equal(p2A.CurrentHp, p2B.CurrentHp);
    }

    [Fact]
    public void ExecuteTurn_WithReversedArguments_RemainsDeterministic()
    {
        var (battle1, p1a, p2a) = CreateBattle(100, 100);
        var (battle2, p1b, p2b) = CreateBattle(100, 100);

        var player1AttackA = Move.BasicAttack(p1a, p2a, 20);
        var player2AttackA = Move.BasicAttack(p2a, p1a, 15);
        var player1AttackB = Move.BasicAttack(p1b, p2b, 20);
        var player2AttackB = Move.BasicAttack(p2b, p1b, 15);

        var resultA = battle1.ExecuteTurn(player1AttackA, player2AttackA);
        var resultB = battle2.ExecuteTurn(player2AttackB, player1AttackB); // Reversed order

        Assert.Equal(resultA.Player1Hp, resultB.Player1Hp);
        Assert.Equal(resultA.Player2Hp, resultB.Player2Hp);
    }

    [Fact]
    public void ExecuteTurn_WhenFirstAttackEndsBattle_SecondAttackIsSkipped()
    {
        var (battle, p1, p2) = CreateBattle(100, 20);

        var player1Attack = Move.BasicAttack(p1, p2, 20);
        var player2Attack = Move.BasicAttack(p2, p1, 15);

        var result = battle.ExecuteTurn(player1Attack, player2Attack);

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
        var validMove = Move.BasicAttack(p1, p2, 10);

        Assert.Throws<ArgumentNullException>(() => battle.ExecuteTurn(null!, validMove));
        Assert.Throws<ArgumentNullException>(() => battle.ExecuteTurn(validMove, null!));
    }

    [Fact]
    public void ExecuteTurn_WithSameAttacker_ThrowsArgumentException()
    {
        var (battle, p1, p2) = CreateBattle();

        var firstMove = Move.BasicAttack(p1, p2, 10);
        var secondMove = Move.BasicAttack(p1, p2, 15);

        Assert.Throws<ArgumentException>(() => battle.ExecuteTurn(firstMove, secondMove));
    }

    [Fact]
    public void ExecuteTurn_WithAttackerNotInBattle_ThrowsArgumentException()
    {
        var (battle, p1, p2) = CreateBattle();
        var outsider = new Creature("Outsider", 100);

        var moveA = Move.BasicAttack(p1, p2, 10);
        var moveB = Move.BasicAttack(outsider, p1, 15);

        Assert.Throws<ArgumentException>(() => battle.ExecuteTurn(moveA, moveB));    
    }

    [Fact]
    public void ExecuteTurn_WhenBattleIsOver_ThrowsInvalidOperationException()
    {
        var (battle, p1, p2) = CreateBattle(100, 10);

        battle.ExecuteAction(p1, p2, 10);

        var move1 = Move.BasicAttack(p1, p2, 1);
        var move2 = Move.BasicAttack(p2, p1, 1);

        Assert.Throws<InvalidOperationException>(() => battle.ExecuteTurn(move1, move2));
    }

    [Fact]
    public void BasicAttack_WithZeroDamage_DoesNotChangeHp()
    {
        var (battle, p1, p2) = CreateBattle(100, 100);

        var zeroDamage = Move.BasicAttack(p1, p2, 0);
        var normalDamage = Move.BasicAttack(p2, p1, 0);

        var result = battle.ExecuteTurn(zeroDamage, normalDamage);

        Assert.Equal(100, p1.CurrentHp);
        Assert.Equal(100, p2.CurrentHp);
        Assert.False(result.BattleEnded);
    }
}