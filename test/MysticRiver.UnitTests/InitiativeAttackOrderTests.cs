using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class InitiativeAttackOrderTests {
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
    private static SelfStatusMove ApplyHaste(Creature self) =>
        new SelfStatusMove(StatusEffect.Haste) { Self = self };
    private static StatusEffectMove ApplySlow(Creature source, Creature destination) =>
        new StatusEffectMove(StatusEffect.Slow) { Source = source, Destination = destination };

    [Fact]
    public void ExecuteTurn_HigherInitiativeAttacksFirst() {
        var (battle, p1, p2) = CreateBattle(10, 10, initiative1: 1, initiative2: 10);

        var result = battle.ExecuteTurn(Attack(p1, p2, 10), Attack(p2, p1, 10));

        Assert.Equal(0, p1.CurrentHp);
        Assert.Equal(10, p2.CurrentHp);
        Assert.True(result.BattleEnded);
        Assert.NotNull(result.FinalResult);
        Assert.Equal(p2, result.FinalResult!.Winner);
        Assert.Equal(p1, result.FinalResult.Loser);
    }

    [Fact]
    public void ExecuteTurn_EqualInitiative_Player1ActsFirst() {
        var (battle, p1, p2) = CreateBattle(10, 10, initiative1: 10, initiative2: 10);

        var result = battle.ExecuteTurn(Attack(p1, p2, 10), Attack(p2, p1, 10));

        Assert.Equal(10, p1.CurrentHp);
        Assert.Equal(0, p2.CurrentHp);
        Assert.True(result.BattleEnded);
        Assert.NotNull(result.FinalResult);
        Assert.Equal(p1, result.FinalResult!.Winner);
        Assert.Equal(p2, result.FinalResult.Loser);
    }

    [Fact]
    public void ExecuteTurn_EqualInitiative_IsDeterministicWhenMoveArgumentsReversed() {
        var (battleA, p1A, p2A) = CreateBattle(30, 30, initiative1: 7, initiative2: 7);
        var (battleB, p1B, p2B) = CreateBattle(30, 30, initiative1: 7, initiative2: 7);

        var resultA = battleA.ExecuteTurn(Attack(p1A, p2A, 12), Attack(p2A, p1A, 9));
        var resultB = battleB.ExecuteTurn(Attack(p2B, p1B, 9), Attack(p1B, p2B, 12)); // reversed

        Assert.Equal(resultA.Creature1Hp, resultB.Creature1Hp);
        Assert.Equal(resultA.Creature2Hp, resultB.Creature2Hp);
        Assert.Equal(p1A.CurrentHp, p1B.CurrentHp);
        Assert.Equal(p2A.CurrentHp, p2B.CurrentHp);
    }

    [Fact]
    public void ExecuteTurn_HigherInitiative_IsDeterministicWhenMoveArgumentsReversed() {
        var (battleA, p1A, p2A) = CreateBattle(hp1: 40, hp2: 40, initiative1: 1, initiative2: 10);
        var (battleB, p1B, p2B) = CreateBattle(hp1: 40, hp2: 40, initiative1: 1, initiative2: 10);

        var resultA = battleA.ExecuteTurn(Attack(p1A, p2A, 12), Attack(p2A, p1A, 9));
        var resultB = battleB.ExecuteTurn(Attack(p2B, p1B, 9), Attack(p1B, p2B, 12)); // reversed

        Assert.Equal(resultA.Creature1Hp, resultB.Creature1Hp);
        Assert.Equal(resultA.Creature2Hp, resultB.Creature2Hp);
        Assert.Equal(resultA.BattleEnded, resultB.BattleEnded);

        if (resultA.BattleEnded) {
            Assert.Equal(resultA.FinalResult!.Winner, resultB.FinalResult!.Winner);
            Assert.Equal(resultA.FinalResult.Loser, resultB.FinalResult.Loser);
        }
    }

    [Fact]
    public void ExecuteTurn_HasteRaisesInitiativeOnNextTurn() {
        var (battle, p1, p2) = CreateBattle(hp1: 10, hp2: 10, initiative1: 10, initiative2: 10);

        battle.ExecuteTurn(
            Attack(p1, p2, 0),
            ApplyHaste(p2));

        battle.ExecuteTurn(
            Attack(p1, p2, 10),
            Attack(p2, p1, 10));

        Assert.Equal(0, p1.CurrentHp);
        Assert.Equal(10, p2.CurrentHp);
    }

    [Fact]
    public void ExecuteTurn_SlowLowersInitiativeOnNextTurn() {
        var (battle, p1, p2) = CreateBattle(hp1: 10, hp2: 10, initiative1: 10, initiative2: 6);

        battle.ExecuteTurn(
            Attack(p1, p2, 0),
            ApplySlow(p2, p1));

        battle.ExecuteTurn(
            Attack(p1, p2, 10),
            Attack(p2, p1, 10));

        Assert.Equal(0, p1.CurrentHp);
        Assert.Equal(10, p2.CurrentHp);
    }

    [Fact]
    public void Constructor_WhenInitiativeIsNegative_ThrowsArgumentOutOfRangeException() {
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() => new Creature("Gruk", 100, -1));
        Assert.Equal("initiative", ex.ParamName);
    }

    [Fact]
    public void Constructor_WhenInitiativeIsZero_AllowsCreation() {
        var creature = new Creature("Gruk", 100, 0);

        Assert.Equal(0, creature.Initiative);
        Assert.False(creature.IsDead);
        Assert.Equal(100, creature.CurrentHp);
    }

    [Fact]
    public void EffectiveInitiative_HasteAndSlowCancelOut() {
        var creature = new Creature("Gruk", 100, 10);

        creature.ApplyStatus(StatusEffect.Haste);
        creature.ApplyStatus(StatusEffect.Slow);

        Assert.Equal(10, creature.EffectiveInitiative);
    }

    [Fact]
    public void EffectiveInitiative_SlowAppliesAlongsideParalysis() {
        var creature = new Creature("Gruk", 100, 10);

        creature.ApplyStatus(StatusEffect.Paralysis);
        creature.ApplyStatus(StatusEffect.Slow);

        Assert.Equal(5, creature.EffectiveInitiative);
        Assert.True(creature.Status.HasFlag(StatusEffect.Paralysis));
        Assert.True(creature.Status.HasFlag(StatusEffect.Slow));
    }
}
