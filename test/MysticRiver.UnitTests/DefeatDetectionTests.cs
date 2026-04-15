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

    [Fact]
    public void TakeDamage_WhenAmountIsZero_ThrowsArgumentOutOfRangeException()
    {
        var creature = new Creature("Gruk", 100);

        var ex =Assert.Throws<ArgumentOutOfRangeException>(() => creature.TakeDamage(0));
        Assert.Equal("amount", ex.ParamName);
    }

    [Fact]
    public void TakeDamage_WhenAmountIsNegative_ThrowsArgumentOutOfRangeException()
    {
        var creature = new Creature("Gruk", 100);

        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => creature.TakeDamage(-1));
        Assert.Equal("amount", ex.ParamName);
    }
}

public class BattleTests
{
    private static (Battle battle, Creature p1, Creature p2) CreateBattle(int hp1 = 100, int hp2 = 100)
    {
        var p1 = new Creature("Creature1", hp1);
        var p2 = new Creature("Creature2", hp2);
        return (new Battle(p1, p2), p1, p2);
    }

    [Fact]
    public void Constructor_WhenCreature1IsNull_ThrowsArgumentNullException()
    {
        var p2 = new Creature("Creature2", 100);

        Assert.Throws<ArgumentNullException>(() => new Battle(null!, p2));
    }

    [Fact]
    public void Constructor_WhenCreature2IsNull_ThrowsArgumentNullException()
    {
        var p1 = new Creature("Creature1", 100);

        Assert.Throws<ArgumentNullException>(() => new Battle(p1, null!));
    }

    [Fact]
    public void Constructor_WhenBothPlayersAreSameInstance_ThrowsArgumentException()
    {
        var p1 = new Creature("Creature1", 100);

        Assert.Throws<ArgumentException>(() => new Battle(p1, p1));
    }

    [Fact]
    public void IsOver_WhenBothAlive_ReturnsFalse()
    {
        var (battle, _, _) = CreateBattle();

        Assert.False(battle.IsOver);
    }

    [Fact]
    public void IsOver_WhenCreature1Dies_ReturnsTrue()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p2, p1, 100);

        Assert.True(battle.IsOver);
    }

    [Fact]
    public void IsOver_WhenCreature2Dies_ReturnsTrue()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p1, p2, 100);

        Assert.True(battle.IsOver);
    }

    [Fact]
    public void TryGetResult_WhileInProgress_ReturnsFalse()
    {
        var (battle, _, _) = CreateBattle();

        Assert.False(battle.TryGetResult(out var result));
        Assert.Null(result);
    }

    [Fact]
    public void TryGetResult_WhenCreature2Dies_ReturnsCreature1AsWinner()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p1, p2, 100);
        Assert.True(battle.TryGetResult(out var result));

        Assert.Equal(p1, result!.Winner);
        Assert.Equal(p2, result.Loser);
    }

    [Fact]
    public void TryGetResult_WhenCreature1Dies_ReturnsCreature2AsWinner()
    {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteAction(p2, p1, 100);
        Assert.True(battle.TryGetResult(out var result));

        Assert.Equal(p2, result!.Winner);
        Assert.Equal(p1, result.Loser);
    }

    [Fact]
    public void ExecuteAction_AfterBattleIsOver_ThrowsInvalidOperationException()
    {
        var (battle, p1, p2) = CreateBattle();
        battle.ExecuteAction(p1, p2, 100);

        Assert.Throws<InvalidOperationException>(() => battle.ExecuteAction(p2, p1, 10));
    }

    [Fact]
    public void ExecuteAction_WhenAttackerNotInBattle_ThrowsArgumentException()
    {
        var (battle, _, p2) = CreateBattle();
        var outsider = new Creature("Outsider", 100);

        Assert.Throws<ArgumentException>(() => battle.ExecuteAction(outsider, p2, 10));
    }

    [Fact]
    public void ExecuteAction_WhenTargetNotInBattle_ThrowsArgumentException()
    {
        var (battle, p1, _) = CreateBattle();
        var outsider = new Creature("Outsider", 100);

        Assert.Throws<ArgumentException>(() => battle.ExecuteAction(p1, outsider, 10));
    }

    [Fact]
    public void ExecuteAction_WhenAttackerAndTargetAreSameCreature_ThrowsArgumentException()
    {
        var (battle, p1, _) = CreateBattle();

        Assert.Throws<ArgumentException>(() => battle.ExecuteAction(p1, p1, 10));
    }
}
