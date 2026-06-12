namespace MysticRiver.Application.Data.Entities;

public sealed class CreatureSnapshotEntity {
    public int Id { get; set; }
    public string BattleId { get; set; } = null!;
    public string CreatureId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Mana { get; set; }
    public int MaxMana { get; set; }
    public int Shield { get; set; }
    public string StatusEffectsJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public BattleSessionEntity? BattleSession { get; set; }
}

