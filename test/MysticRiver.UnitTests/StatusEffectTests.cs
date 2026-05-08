using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class StatusEffectTests
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

    private static StatusDamageMove StatusAttack(Creature source, Creature destination, int damage, StatusEffect effect) =>
        new(damage, DamageKind.Physical, effect) { Source = source, Destination = destination };
    // ── ApplyStatus ──────────────────────────────────────────────────────────

    [Fact]
    public void ApplyStatus_SetsStatusOnCreature()
    {
        var creature = new Creature("Gruk", 100, 10);

        creature.ApplyStatus(StatusEffect.Poison);

        Assert.Equal(StatusEffect.Poison, creature.Status);
    }

    [Fact]
    public void ApplyStatus_DefaultStatus_IsNull()
    {
        var creature = new Creature("Gruk", 100, 10);

        Assert.Null(creature.Status);
    }

    [Fact]
    public void ApplyStatus_OverwritesPreviousStatus()
    {
        var creature = new Creature("Gruk", 100, 10);
        creature.ApplyStatus(StatusEffect.Poison);

        creature.ApplyStatus(StatusEffect.Burn);

        Assert.Equal(StatusEffect.Burn, creature.Status);
    }

    [Fact]
    public void ClearStatus_ResetsStatusToNull()
    {
        var creature = new Creature("Gruk", 100, 10);
        creature.ApplyStatus(StatusEffect.Poison);

        creature.ClearStatus();

        Assert.Null(creature.Status);
    }

    // ── StatusAttack move ────────────────────────────────────────────────────

    [Fact]
    public void StatusAttack_Move_HasCorrectInflictedStatus()
    {
        var p1 = new Creature("Creature1", 100, 10);
        var p2 = new Creature("Creature2", 100, 10);

        var move = StatusAttack(p1, p2, 20, StatusEffect.Poison);

        Assert.Equal(StatusEffect.Poison, move.Effect);
    }

    [Fact]
    public void BasicAttack_Move_IsDamageMove()
    {
        var p1 = new Creature("Creature1", 100, 10);
        var p2 = new Creature("Creature2", 100, 10);

        var move = Attack(p1, p2, 20);

        Assert.Equal(20, move.DamageAmount);
    }

    // ── Status applied during battle turn ────────────────────────────────────

    [Fact]
    public void ExecuteTurn_StatusAttack_AppliesStatusToTarget()
    {
        var (battle, p1, p2) = CreateBattle();

        var statusMove = StatusAttack(p1, p2, 10, StatusEffect.Poison);
        var normalMove = Attack(p2, p1, 10);

        battle.ExecuteTurn(statusMove, normalMove);

        Assert.Equal(StatusEffect.Poison, p2.Status);
    }

    [Fact]
    public void ExecuteTurn_BasicAttack_DoesNotApplyStatusToTarget()
    {
        var (battle, p1, p2) = CreateBattle();

        var move1 = Attack(p1, p2, 10);
        var move2 = Attack(p2, p1, 10);

        battle.ExecuteTurn(move1, move2);

        Assert.Null(p2.Status);
    }

    [Fact]
    public void ExecuteTurn_StatusIsNotAppliedToDeadTarget()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 10);

        var killMove = StatusAttack(p1, p2, 10, StatusEffect.Poison);
        var normalMove = Attack(p2, p1, 5);

        battle.ExecuteTurn(killMove, normalMove);

        Assert.True(p2.IsDead);
        Assert.Null(p2.Status);
    }

    // ── Status ticks on next turn ─────────────────────────────────────────────

    [Fact]
    public void Poison_TicksOnNextTurn_DealsTwelfthPointFivePercentDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: p1 attacks p2 for 1 → p2 HP = 99; poison applied
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 1, StatusEffect.Poison),
            Attack(p2, p1, 1));

        Assert.Equal(StatusEffect.Poison, p2.Status);

        // Turn 2: poison tick = MaxHp/8 = 12; p1 attacks for 1 → p2 HP = 99 - 12 - 1 = 86
        battle.ExecuteTurn(
            Attack(p1, p2, 1),
            Attack(p2, p1, 1));

        Assert.Equal(86, p2.CurrentHp);
    }

    [Fact]
    public void Burn_TicksOnNextTurn_DealsSixPointTwentyFivePercentDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: p1 attacks p2 for 1 → p2 HP = 99; burn applied
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 1, StatusEffect.Burn),
            Attack(p2, p1, 1));

        // Turn 2: burn tick = MaxHp/16 = 6; p1 attacks for 1 → p2 HP = 99 - 6 - 1 = 92
        battle.ExecuteTurn(
            Attack(p1, p2, 1),
            Attack(p2, p1, 1));

        Assert.Equal(92, p2.CurrentHp);
    }

    [Fact]
    public void NoStatus_TurnTickCausesNoDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: no status
        battle.ExecuteTurn(
            Attack(p1, p2, 1),
            Attack(p2, p1, 1));

        var hpAfterTurn1 = p2.CurrentHp; // 99

        // Turn 2: no status tick expected
        battle.ExecuteTurn(
            Attack(p1, p2, 1),
            Attack(p2, p1, 1));

        // only 1 attack damage, no tick
        Assert.Equal(hpAfterTurn1 - 1, p2.CurrentHp);
    }

    [Fact]
    public void Paralysis_DoesNotTickDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: apply paralysis
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 1, StatusEffect.Paralysis),
            Attack(p2, p1, 1));

        var hpAfterTurn1 = p2.CurrentHp; // 99

        // Turn 2: paralysis has no HP tick
        battle.ExecuteTurn(
            Attack(p1, p2, 1),
            Attack(p2, p1, 1));

        // only 1 attack damage, no tick
        Assert.Equal(hpAfterTurn1 - 1, p2.CurrentHp);
    }

    // ── TurnResult status snapshot ────────────────────────────────────────────

    [Fact]
    public void TurnResult_ReflectsAppliedStatusAfterTurn()
    {
        var (battle, p1, p2) = CreateBattle();

        var result = battle.ExecuteTurn(
            StatusAttack(p1, p2, 10, StatusEffect.Poison),
            Attack(p2, p1, 10));

        Assert.Equal(StatusEffect.Poison, result.Creature2Status);
        Assert.Null(result.Creature1Status);
    }

    [Fact]
    public void TurnResult_StatusIsNullWhenNoStatusApplied()
    {
        var (battle, p1, p2) = CreateBattle();

        var result = battle.ExecuteTurn(
            Attack(p1, p2, 10),
            Attack(p2, p1, 10));

        Assert.Null(result.Creature1Status);
        Assert.Null(result.Creature2Status);
    }

    // ── Paralysis: skips move for 2 turns ────────────────────────────────────

    [Fact]
    public void Paralysis_SkipsCreatureMove_ForTwoTurns()
    {
        // p1 has higher initiative so it always moves first
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: p1 applies Paralysis then p2's move is consumed (skip 1/2)
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 5, StatusEffect.Paralysis),
            Attack(p2, p1, 10));

        Assert.Equal(100, p1.CurrentHp); // p2 was skipped
        Assert.Equal(95, p2.CurrentHp);
        Assert.Equal(StatusEffect.Paralysis, p2.Status);

        // Turn 2: second skip, Paralysis clears afterward
        battle.ExecuteTurn(
            Attack(p1, p2, 5),
            Attack(p2, p1, 10));

        Assert.Equal(100, p1.CurrentHp); // p2 was skipped again
        Assert.Equal(90, p2.CurrentHp);
        Assert.Null(p2.Status);

        // Turn 3: p2 can act normally
        battle.ExecuteTurn(
            Attack(p1, p2, 5),
            Attack(p2, p1, 10));

        Assert.Equal(90, p1.CurrentHp); // p2 attacked
        Assert.Equal(85, p2.CurrentHp);
    }

    // ── Sleep: skips move for 2 turns, wakes on damage ───────────────────────

    [Fact]
    public void Sleep_ClearsAfterTwoTurns_WhenNoDamageDealt()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: Sleep applied; p2's move skipped (skip 1/2)
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 5, StatusEffect.Sleep),
            Attack(p2, p1, 10));

        Assert.Equal(StatusEffect.Sleep, p2.Status);
        Assert.Equal(100, p1.CurrentHp);

        // Turn 2: p1 restores mana (no damage to p2) so p2 stays asleep; skip 2/2
        battle.ExecuteTurn(
            new ManaRestoreMove(1) { Self = p1 },
            Attack(p2, p1, 10));

        Assert.Null(p2.Status);          // Sleep expired naturally
        Assert.Equal(100, p1.CurrentHp); // p2 was still asleep
    }

    [Fact]
    public void Sleep_WakesOnDamage_AllowingCreatureToActSameTurn()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: Sleep applied; p2's move skipped
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 5, StatusEffect.Sleep),
            Attack(p2, p1, 10));

        Assert.Equal(StatusEffect.Sleep, p2.Status);

        // Turn 2: p1 attacks p2 → p2 wakes up → p2 can act in the same turn
        battle.ExecuteTurn(
            Attack(p1, p2, 5),
            Attack(p2, p1, 10));

        Assert.Null(p2.Status);          // woke up from damage
        Assert.Equal(90, p1.CurrentHp);  // p2 attacked after waking
    }

    // ── Freeze: 1-turn effect, 15 % chance to skip ───────────────────────────

    [Fact]
    public void Freeze_SkipsTurn_WhenRollBelow15Percent()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2, rollSkip: () => 0.10);

        // Turn 1: p1 applies Freeze; roll 0.10 < 0.15 → p2 skips
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 5, StatusEffect.Freeze),
            Attack(p2, p1, 10));

        Assert.Equal(100, p1.CurrentHp); // p2 was frozen (skipped)
        Assert.Null(p2.Status);           // Freeze always clears after one turn
    }

    [Fact]
    public void Freeze_AllowsAction_WhenRollAtOrAbove15Percent()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2, rollSkip: () => 0.50);

        // Turn 1: p1 applies Freeze; roll 0.50 >= 0.15 → p2 still acts
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 5, StatusEffect.Freeze),
            Attack(p2, p1, 10));

        Assert.Equal(90, p1.CurrentHp);  // p2 was not frozen (acted normally)
        Assert.Null(p2.Status);           // Freeze always clears after one turn
    }

    [Fact]
    public void Freeze_ClearsAfterOneTurn_WhenRollSkips()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2, rollSkip: () => 0.10);

        // Turn 1: Freeze applied and consumed (skip)
        battle.ExecuteTurn(
            StatusAttack(p1, p2, 5, StatusEffect.Freeze),
            Attack(p2, p1, 10));

        Assert.Null(p2.Status);

        // Turn 2: p2 has no status, acts normally regardless of roll
        battle.ExecuteTurn(
            Attack(p1, p2, 5),
            Attack(p2, p1, 10));

        Assert.Equal(90, p1.CurrentHp); // p2 attacked in turn 2
    }
}
