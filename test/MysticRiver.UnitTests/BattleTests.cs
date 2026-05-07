using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class BattleTests {
    private static (Battle battle, Creature p1, Creature p2) CreateBattle(
        int hp1 = 100,
        int hp2 = 100,
        int initiative1 = 10,
        int initiative2 = 20) {
        var p1 = new Creature("Creature1", hp1, initiative1);
        var p2 = new Creature("Creature2", hp2, initiative2);
        return (new Battle(p1, p2), p1, p2);
    }

    private static DamageMove Attack(Creature source, Creature destination, int damage) =>
        new DamageMove(damage, DamageKind.Physical) { Source = source, Destination = destination };

    [Fact]
    public void Constructor_WhenCreature1IsNull_ThrowsArgumentNullException() {
        var p2 = new Creature("Creature2", 100, 20);

        Assert.Throws<ArgumentNullException>(() => new Battle(null!, p2));
    }

    [Fact]
    public void Constructor_WhenCreature2IsNull_ThrowsArgumentNullException() {
        var p1 = new Creature("Creature1", 100, 10);

        Assert.Throws<ArgumentNullException>(() => new Battle(p1, null!));
    }

    [Fact]
    public void Constructor_WhenBothPlayersAreSameInstance_ThrowsArgumentException() {
        var p1 = new Creature("Creature1", 100, 10);

        Assert.Throws<ArgumentException>(() => new Battle(p1, p1));
    }

    [Fact]
    public void IsOver_WhenBothAlive_ReturnsFalse() {
        var (battle, _, _) = CreateBattle();

        Assert.False(battle.IsOver);
    }

    [Fact]
    public void IsOver_WhenCreature1Dies_ReturnsTrue() {
        // p2 has higher initiative (20) so attacks first -> kills p1 before p1 can retaliate
        var (battle, p1, p2) = CreateBattle(hp1: 10, hp2: 100);

        battle.ExecuteTurn(Attack(p1, p2, 1), Attack(p2, p1, 100));

        Assert.True(battle.IsOver);
    }

    [Fact]
    public void IsOver_WhenCreature2Dies_ReturnsTrue() {
        // p2 attacks first but deals non-lethal damage; p1 survives and kills p2
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 10);

        battle.ExecuteTurn(Attack(p1, p2, 100), Attack(p2, p1, 1));

        Assert.True(battle.IsOver);
    }

    [Fact]
    public void TryGetResult_WhileInProgress_ReturnsFalse() {
        var (battle, _, _) = CreateBattle();

        Assert.False(battle.TryGetResult(out var result));
        Assert.Null(result);
    }

    [Fact]
    public void TryGetResult_WhenCreature2Dies_ReturnsCreature1AsWinner() {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 10);

        battle.ExecuteTurn(Attack(p1, p2, 100), Attack(p2, p1, 1));

        Assert.True(battle.TryGetResult(out var result));
        Assert.Equal(p1, result!.Winner);
        Assert.Equal(p2, result.Loser);
    }

    [Fact]
    public void TryGetResult_WhenCreature1Dies_ReturnsCreature2AsWinner() {
        // p2 has higher initiative, kills p1 first
        var (battle, p1, p2) = CreateBattle(hp1: 10, hp2: 100);

        battle.ExecuteTurn(Attack(p1, p2, 1), Attack(p2, p1, 100));

        Assert.True(battle.TryGetResult(out var result));
        Assert.Equal(p2, result!.Winner);
        Assert.Equal(p1, result.Loser);
    }

    [Fact]
    public void ExecuteTurn_AfterBattleIsOver_ThrowsInvalidOperationException() {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 10);

        battle.ExecuteTurn(Attack(p1, p2, 100), Attack(p2, p1, 1));

        Assert.Throws<InvalidOperationException>(() =>
            battle.ExecuteTurn(Attack(p1, p2, 1), Attack(p2, p1, 1)));
    }

    [Fact]
    public void ExecuteTurn_WhenAttackerNotInBattle_ThrowsArgumentException() {
        var (battle, p1, p2) = CreateBattle();
        var outsider = new Creature("Outsider", 100, 10);

        Assert.Throws<ArgumentException>(() =>
            battle.ExecuteTurn(Attack(p1, p2, 10), Attack(outsider, p1, 15)));
    }

    [Fact]
    public void ExecuteTurn_WhenTargetNotInBattle_ThrowsArgumentException() {
        var (battle, p1, p2) = CreateBattle();
        var outsider = new Creature("Outsider", 100, 10);

        Assert.Throws<ArgumentException>(() =>
            battle.ExecuteTurn(Attack(p1, p2, 10), Attack(p2, outsider, 15)));
    }

    [Fact]
    public void ExecuteTurn_WhenSourceAndDestinationAreTheSameCreature_ThrowsArgumentException() {
        var (battle, p1, p2) = CreateBattle();

        Assert.Throws<ArgumentException>(() =>
            battle.ExecuteTurn(Attack(p1, p2, 10), Attack(p2, p2, 10)));
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
}
