namespace MysticRiver.Application.Data.Entities;

public sealed class BattleSessionEntity {
    public string BattleId { get; set; } = null!;
    public string? HostPlayerId { get; set; }
    public string? GuestPlayerId { get; set; }
    public int MatchStatus { get; set; }
    public int RoundNumber { get; set; }
    public int StateVersion { get; set; }
    public int EnemyAttackPower { get; set; }
    public string? CurrentTurnCreatureId { get; set; }
    public string? ForcedWinnerCreatureId { get; set; }
    public int ForcedEndReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<CreatureSnapshotEntity> CreatureSnapshots { get; set; } = new List<CreatureSnapshotEntity>();
}

