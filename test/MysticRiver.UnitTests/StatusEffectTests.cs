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
    public void ApplyStatus_DefaultStatus_IsNone()
    {
        var creature = new Creature("Gruk", 100, 10);

        Assert.Equal(StatusEffect.None, creature.Status);
    }

    [Fact]
    public void ApplyStatus_OverwritesPreviousStatus()
    {
        var creature = new Creature("Gruk", 100, 10);
        creature.ApplyStatus(StatusEffect.Poison);

        creature.ApplyStatus(StatusEffect.Burn);

        Assert.Equal(StatusEffect.Burn, creature.Status);
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
    public void BasicAttack_Move_HasInflictedStatusNone()
    {
        var p1 = new Creature("Creature1", 100, 10);
        var p2 = new Creature("Creature2", 100, 10);

        var move = Move.BasicAttack(p1, p2, 20);

        Assert.Equal(StatusEffect.None, move.InflictedStatus);
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

        Assert.Equal(StatusEffect.None, p2.Status);
    }

    [Fact]
    public void ExecuteTurn_StatusIsNotAppliedToDeadTarget()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 10);

        var killMove = Move.StatusAttack(p1, p2, 10, StatusEffect.Poison);
        var normalMove = Move.BasicAttack(p2, p1, 5);

        battle.ExecuteTurn(killMove, normalMove);

        Assert.True(p2.IsDead);
        Assert.Equal(StatusEffect.None, p2.Status);
    }

    // ── Status ticks on next turn ─────────────────────────────────────────────

    [Fact]
    public void Poison_TicksOnNextTurn_DealsTwelfthPointFivePercentDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: apply poison to p2
        var statusMove = Move.StatusAttack(p1, p2, 1, StatusEffect.Poison);
        var noOpMove = Move.BasicAttack(p2, p1, 1);
        battle.ExecuteTurn(statusMove, noOpMove);

        Assert.Equal(StatusEffect.Poison, p2.Status);
        var hpAfterTurn1 = p2.CurrentHp; // 99 (took 1 damage)

        // Turn 2: poison ticks (100 / 8 = 12) + p1 attacks for 1 = 13 total damage
        var move1 = Move.BasicAttack(p1, p2, 1);
        var move2 = Move.BasicAttack(p2, p1, 1);
        battle.ExecuteTurn(move1, move2);

        Assert.Equal(hpAfterTurn1 - 12 - 1, p2.CurrentHp);
    }

    [Fact]
    public void Burn_TicksOnNextTurn_DealsSixPointTwentyFivePercentDamage()
    {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);

        // Turn 1: apply burn to p2
        battle.ExecuteTurn(
            Move.StatusAttack(p1, p2, 1, StatusEffect.Burn),
            Move.BasicAttack(p2, p1, 1));

        var hpAfterTurn1 = p2.CurrentHp; // 99 (took 1 damage)

        // Turn 2: burn ticks (100 / 16 = 6) + p1 attacks for 1 = 7 total damage
        battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 1),
            Move.BasicAttack(p2, p1, 1));

        Assert.Equal(hpAfterTurn1 - 6 - 1, p2.CurrentHp);
    }

    [Fact]
    public void NoStatus_TurnTickCauseNoDamage()
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
        Assert.Equal(StatusEffect.None, result.Creature1Status);
    }

    [Fact]
    public void TurnResult_StatusIsNoneWhenNoStatusApplied()
    {
        var (battle, p1, p2) = CreateBattle();

        var result = battle.ExecuteTurn(
            Move.BasicAttack(p1, p2, 10),
            Move.BasicAttack(p2, p1, 10));

        Assert.Equal(StatusEffect.None, result.Creature1Status);
        Assert.Equal(StatusEffect.None, result.Creature2Status);
    }
}
