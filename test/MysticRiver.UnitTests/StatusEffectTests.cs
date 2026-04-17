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

        var move = Move.StatusAttack(p1, p2, 20, StatusEffect.Poison);

        Assert.Equal(StatusEffect.Poison, move.InflictedStatus);
    }

    [Fact]
    public void BasicAttack_Move_HasNullInflictedStatus()
    {
        var p1 = new Creature("Creature1", 100, 10);
        var p2 = new Creature("Creature2", 100, 10);

        var move = Move.BasicAttack(p1, p2, 20);

        Assert.Null(move.InflictedStatus);
    }

    // ── Status applied during battle turn ────────────────────────────────────

    [Fact]
    public void ExecuteTurn_StatusAttack_AppliesStatusToTarget()
    {
        var (battle, p1, p2) = CreateBattle();

        var statusMove = Move.StatusAttack(p1, p2, 10, StatusEffect.Poison);
        var normalMove = Move.BasicAttack(p2, p1, 10);

        battle.ExecuteTurn(statusMove, normalMove);

        Assert.Equal(StatusEffect.Poison, p2.Status);
    }

    [Fact]
    public void ExecuteTurn_BasicAttack_DoesNotApplyStatusToTarget()
    {
        var (battle, p1, p2) = CreateBattle();

        var move1 = Move.BasicAttack(p1, p2, 10);
        var move2 = Move.BasicAttack(p2, p1, 10);

        battle.ExecuteTurn(move1, move2);

        Assert.Null(p2.Status);
    }

    [Fact]
    public void ExecuteTurn_StatusIsNotAppliedToDeadTarget()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 10);

        var killMove = Move.StatusAttack(p1, p2, 10, StatusEffect.Poison);
        var normalMove = Move.BasicAttack(p2, p1, 5);

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
            Move.StatusAttack(p1, p2, 1, StatusEffect.Poison),
            Move.BasicAttack(p2, p1, 1));

        Assert.Equal(StatusEffect.Poison, p2.Status);

        // Turn 2: poison tick = MaxHp/8 = 12; p1 attacks for 1 → p2 HP = 99 - 12 - 1 = 86
        battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 1),
            Move.BasicAttack(p2, p1, 1));

        Assert.Equal(86, p2.CurrentHp);
    }

    [Fact]
    public void Burn_TicksOnNextTurn_DealsSixPointTwentyFivePercentDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: p1 attacks p2 for 1 → p2 HP = 99; burn applied
        battle.ExecuteTurn(
            Move.StatusAttack(p1, p2, 1, StatusEffect.Burn),
            Move.BasicAttack(p2, p1, 1));

        // Turn 2: burn tick = MaxHp/16 = 6; p1 attacks for 1 → p2 HP = 99 - 6 - 1 = 92
        battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 1),
            Move.BasicAttack(p2, p1, 1));

        Assert.Equal(92, p2.CurrentHp);
    }

    [Fact]
    public void NoStatus_TurnTickCausesNoDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: no status
        battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 1),
            Move.BasicAttack(p2, p1, 1));

        var hpAfterTurn1 = p2.CurrentHp; // 99

        // Turn 2: no status tick expected
        battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 1),
            Move.BasicAttack(p2, p1, 1));

        // only 1 attack damage, no tick
        Assert.Equal(hpAfterTurn1 - 1, p2.CurrentHp);
    }

    [Fact]
    public void Paralysis_DoesNotTickDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: apply paralysis
        battle.ExecuteTurn(
            Move.StatusAttack(p1, p2, 1, StatusEffect.Paralysis),
            Move.BasicAttack(p2, p1, 1));

        var hpAfterTurn1 = p2.CurrentHp; // 99

        // Turn 2: paralysis has no HP tick
        battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 1),
            Move.BasicAttack(p2, p1, 1));

        // only 1 attack damage, no tick
        Assert.Equal(hpAfterTurn1 - 1, p2.CurrentHp);
    }

    // ── TurnResult status snapshot ────────────────────────────────────────────

    [Fact]
    public void TurnResult_ReflectsAppliedStatusAfterTurn()
    {
        var (battle, p1, p2) = CreateBattle();

        var result = battle.ExecuteTurn(
            Move.StatusAttack(p1, p2, 10, StatusEffect.Poison),
            Move.BasicAttack(p2, p1, 10));

        Assert.Equal(StatusEffect.Poison, result.Creature2Status);
        Assert.Null(result.Creature1Status);
    }

    [Fact]
    public void TurnResult_StatusIsNullWhenNoStatusApplied()
    {
        var (battle, p1, p2) = CreateBattle();

        var result = battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 10),
            Move.BasicAttack(p2, p1, 10));

        Assert.Null(result.Creature1Status);
        Assert.Null(result.Creature2Status);
    }
}
