using MysticRiver.Domain;

namespace MysticRiver.UnitTests;

public class HealMoveTests {
    private static (Battle battle, Creature p1, Creature p2) CreateBattle(
        int hp1 = 100,
        int hp2 = 100,
        int initiative1 = 20,
        int initiative2 = 10) {
        var p1 = new Creature("Creature1", hp1, initiative1);
        var p2 = new Creature("Creature2", hp2, initiative2);
        return (new Battle(p1, p2), p1, p2);
    }

    private static DamageMove Attack(Creature source, Creature destination, int damage) =>
        new DamageMove(damage, DamageKind.Physical) { Source = source, Destination = destination };

    private static HealMove Heal(Creature self, int healAmount, int manaCost = 10) =>
        new HealMove(healAmount, manaCost) { Self = self };

    [Fact]
    public void HealMove_RestoresHp() {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);
        p1.TakeDamage(30, DamageKind.Physical); // CurrentHp = 70, MaxHp stays 100

        battle.ExecuteTurn(Heal(p1, 20), Attack(p2, p1, 1));

        Assert.Equal(89, p1.CurrentHp); // 70 + 20 healed, then -1 from p2's attack
    }

    [Fact]
    public void HealMove_CannotExceedMaxHp() {
        var (battle, p1, p2) = CreateBattle(hp1: 100, hp2: 100);
        p1.TakeDamage(5, DamageKind.Physical); // CurrentHp = 95, MaxHp stays 100

        battle.ExecuteTurn(Heal(p1, 50), Attack(p2, p1, 1));

        Assert.Equal(99, p1.CurrentHp); // capped at 100, then -1 from p2
    }

    [Fact]
    public void HealMove_ConsumesManaCost() {
        var (battle, p1, p2) = CreateBattle();

        battle.ExecuteTurn(Heal(p1, 10, manaCost: 30), Attack(p2, p1, 1));

        Assert.Equal(70, p1.CurrentMana);
    }

    [Fact]
    public void HealMove_WhenInsufficientMana_DoesNotHeal() {
        var p1 = new Creature("Creature1", 100, 20, maxMana: 10);
        var p2 = new Creature("Creature2", 100, 10);
        p1.TakeDamage(30, DamageKind.Physical); // CurrentHp = 70
        var battle = new Battle(p1, p2);

        battle.ExecuteTurn(Heal(p1, 20, manaCost: 50), Attack(p2, p1, 1));

        Assert.Equal(69, p1.CurrentHp); // no heal, only p2's attack applied
        Assert.Equal(10, p1.CurrentMana); // mana unchanged
    }
}
