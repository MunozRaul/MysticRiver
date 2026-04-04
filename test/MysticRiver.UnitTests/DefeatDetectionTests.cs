using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class CreatureTests
{
    [Fact]
    public void IsDead_WhenHpAboveZero_ReturnsFalse()
    {
        var creature = new Creature("Gruk", 100);

        Assert.False(creature.IsDead);
    }

    [Fact]
    public void IsDead_WhenHpReducedToZero_ReturnsTrue()
    {
        var creature = new Creature("Gruk", 100);

        creature.TakeDamage(100);

        Assert.True(creature.IsDead);
    }

    [Fact]
    public void IsDead_WhenDamageExceedsHp_ReturnsTrueAndHpIsZero()
    {
        var creature = new Creature("Gruk", 50);

        creature.TakeDamage(200);

        Assert.True(creature.IsDead);
        Assert.Equal(0, creature.CurrentHp);
    }

    [Fact]
    public void TakeDamage_ReducesHpByAmount()
    {
        var creature = new Creature("Gruk", 100);

        creature.TakeDamage(30);

        Assert.Equal(70, creature.CurrentHp);
    }
}

public class BattleTests
{
    private static (Battle battle, Creature p1, Creature p2) CreateBattle(int hp1 = 100, int hp2 = 100)
    {
        var p1 = new Creature("Player1", hp1);
        var p2 = new Creature("Player2", hp2);
        return (new Battle(p1, p2), p1, p2);
    }

    [Fact]
    public void IsOver_WhenBothAlive_ReturnsFalse()
    {
        var (battle, _, _) = CreateBattle();

        Assert.False(battle.IsOver);
    }

    [Fact]
    public void IsOver_WhenPlayer1Dies_ReturnsTrue()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p2, p1, 100);

        Assert.True(battle.IsOver);
    }

    [Fact]
    public void IsOver_WhenPlayer2Dies_ReturnsTrue()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p1, p2, 100);

        Assert.True(battle.IsOver);
    }

    [Fact]
    public void GetResult_WhileInProgress_ReturnsNull()
    {
        var (battle, _, _) = CreateBattle();

        Assert.Null(battle.GetResult());
    }

    [Fact]
    public void GetResult_WhenPlayer2Dies_ReturnsPlayer1AsWinner()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p1, p2, 100);
        var result = battle.GetResult();

        Assert.NotNull(result);
        Assert.Equal(p1, result.Winner);
        Assert.Equal(p2, result.Loser);
    }

    [Fact]
    public void GetResult_WhenPlayer1Dies_ReturnsPlayer2AsWinner()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p2, p1, 100);
        var result = battle.GetResult();

        Assert.NotNull(result);
        Assert.Equal(p2, result.Winner);
        Assert.Equal(p1, result.Loser);
    }

    [Fact]
    public void ExecuteAction_AfterBattleIsOver_ThrowsInvalidOperationException()
    {
        var (battle, p1, p2) = CreateBattle();
        battle.ExecuteAction(p1, p2, 100);

        Assert.Throws<InvalidOperationException>(() => battle.ExecuteAction(p2, p1, 10));
    }
}
