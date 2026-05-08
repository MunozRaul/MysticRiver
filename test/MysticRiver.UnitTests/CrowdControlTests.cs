using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class CrowdControlTests
{

    private static DamageMove Attack(Creature source, Creature destination, int damage) =>
        new(damage, DamageKind.Physical) { Source = source, Destination = destination };

    private static CrowdControlMove CrowdControl(Creature source, Creature destination, CrowdControlKind kind, int turns = 2) =>
        new(turns, kind) { Source = source, Destination = destination };

    [Fact]
    public void Silence_AppliedViaCrowdControlMove_SetsCrowdControlState()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(
            CrowdControl(p1, p2, CrowdControlKind.Silence),
            Attack(p2, p1, 10));

        Assert.Equal(CrowdControlKind.Silence, p2.CrowdControl);
        Assert.Equal(1, p2.CrowdControlTurnsRemaining);
    }

    [Fact]
    public void Silence_BlocksHealMove()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: damage p2
        battle.ExecuteTurn(
            Attack(p1, p2, 30),
            Attack(p2, p1, 1));

        Assert.Equal(70, p2.CurrentHp);

        // Turn 2: p1 silences p2; p2 tries to heal — blocked
        battle.ExecuteTurn(
            CrowdControl(p1, p2, CrowdControlKind.Silence),
            new HealMove(50, 10) { Self = p2 });

        Assert.Equal(70, p2.CurrentHp); // heal was blocked
        Assert.Equal(CrowdControlKind.Silence, p2.CrowdControl);
    }

    [Fact]
    public void Silence_BlocksShieldMove()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: silence p2; p2 tries to apply a shield — blocked
        battle.ExecuteTurn(
            CrowdControl(p1, p2, CrowdControlKind.Silence),
            new ShieldMove(50, 10) { Self = p2 });

        // Turn 2: attack p2 — no shield absorbed because ShieldMove was blocked
        battle.ExecuteTurn(
            Attack(p1, p2, 20),
            Attack(p2, p1, 10));

        Assert.Equal(80, p2.CurrentHp); // 100 - 20, no shield
    }

    [Fact]
    public void Silence_AllowsNonManaMoves()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: silence p2; p2 uses a basic attack (no mana cost) — should still work
        battle.ExecuteTurn(
            CrowdControl(p1, p2, CrowdControlKind.Silence),
            Attack(p2, p1, 10));

        Assert.Equal(90, p1.CurrentHp); // p2's attack landed
    }

    [Fact]
    public void Silence_ClearsAfterTwoTurns()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: silence applied; tick 1/2
        battle.ExecuteTurn(
            CrowdControl(p1, p2, CrowdControlKind.Silence),
            Attack(p2, p1, 1));

        Assert.Equal(CrowdControlKind.Silence, p2.CrowdControl);

        // Turn 2: tick 2/2 → silence clears
        battle.ExecuteTurn(
            Attack(p1, p2, 1),
            Attack(p2, p1, 1));

        Assert.Null(p2.CrowdControl);

        // Turn 3: p2 can heal freely
        var p2HpBeforeHeal = p2.CurrentHp;
        battle.ExecuteTurn(
            Attack(p1, p2, 1),
            new HealMove(20, 10) { Self = p2 });

        Assert.True(p2.CurrentHp > p2HpBeforeHeal - 1); // heal applied
    }

    [Fact]
    public void Stun_BlocksEntireMove()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: p1 stuns p2; p2's attack should be blocked
        battle.ExecuteTurn(
            CrowdControl(p1, p2, CrowdControlKind.Stun),
            Attack(p2, p1, 10));

        Assert.Equal(100, p1.CurrentHp); // p2 was stunned (did not attack)
        Assert.Equal(CrowdControlKind.Stun, p2.CrowdControl);
    }

    [Fact]
    public void Stun_ClearsAfterTwoTurns()
    {
        var p1 = new Creature("p1", 100, 20);
        var p2 = new Creature("p2", 100, 10);
        var battle = new Battle(p1, p2);

        // Turn 1: apply stun
        battle.ExecuteTurn(
            CrowdControl(p1, p2, CrowdControlKind.Stun),
            Attack(p2, p1, 10));

        Assert.Equal(CrowdControlKind.Stun, p2.CrowdControl);

        // Turn 2: still stunned -> skipped
        battle.ExecuteTurn(
            Attack(p1, p2, 10),
            Attack(p2, p1, 10));

        Assert.Null(p2.CrowdControl); // stun expired after turn 2

        // Turn 3: stun cleared -> p2 can act
        battle.ExecuteTurn(
            Attack(p1, p2, 10),
            Attack(p2, p1, 10));

        Assert.Equal(90, p1.CurrentHp); // p2 attacked in turn 3
    }

    [Fact]
    public void ApplyCrowdControl_OverwritesExistingCrowdControl()
    {
        var creature = new Creature("c", 100, 10);

        creature.ApplyCrowdControl(CrowdControlKind.Silence, 2);
        Assert.Equal(CrowdControlKind.Silence, creature.CrowdControl);
        Assert.Equal(2, creature.CrowdControlTurnsRemaining);

        creature.ApplyCrowdControl(CrowdControlKind.Stun, 3);
        Assert.Equal(CrowdControlKind.Stun, creature.CrowdControl);
        Assert.Equal(3, creature.CrowdControlTurnsRemaining);
    }
}
