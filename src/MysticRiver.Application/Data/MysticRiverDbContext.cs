using Microsoft.EntityFrameworkCore;
using MysticRiver.Application.Data.Entities;

namespace MysticRiver.Application.Data;

public sealed class MysticRiverDbContext : DbContext {
    public DbSet<BattleSessionEntity> BattleSessions { get; set; } = null!;
    public DbSet<CreatureSnapshotEntity> CreatureSnapshots { get; set; } = null!;

    public MysticRiverDbContext(DbContextOptions<MysticRiverDbContext> options) : base(options) {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        base.OnModelCreating(modelBuilder);

        var battleSessionBuilder = modelBuilder.Entity<BattleSessionEntity>();
        battleSessionBuilder.HasKey(x => x.BattleId);
        battleSessionBuilder.Property(x => x.BattleId).ValueGeneratedNever();
        battleSessionBuilder.Property(x => x.MatchStatus).IsRequired();
        battleSessionBuilder.Property(x => x.RoundNumber).IsRequired();
        battleSessionBuilder.Property(x => x.StateVersion).IsRequired();
        battleSessionBuilder.Property(x => x.EnemyAttackPower).IsRequired();
        battleSessionBuilder.Property(x => x.CreatedAt).IsRequired();
        battleSessionBuilder.Property(x => x.UpdatedAt).IsRequired();

        var creatureSnapshotBuilder = modelBuilder.Entity<CreatureSnapshotEntity>();
        creatureSnapshotBuilder.HasKey(x => x.Id);
        creatureSnapshotBuilder.Property(x => x.Id).ValueGeneratedOnAdd();
        creatureSnapshotBuilder.Property(x => x.BattleId).IsRequired();
        creatureSnapshotBuilder.Property(x => x.CreatureId).IsRequired();
        creatureSnapshotBuilder.Property(x => x.Name).IsRequired();
        creatureSnapshotBuilder.Property(x => x.Hp).IsRequired();
        creatureSnapshotBuilder.Property(x => x.MaxHp).IsRequired();
        creatureSnapshotBuilder.Property(x => x.Mana).IsRequired();
        creatureSnapshotBuilder.Property(x => x.MaxMana).IsRequired();
        creatureSnapshotBuilder.Property(x => x.Shield).IsRequired();
        creatureSnapshotBuilder.Property(x => x.CreatedAt).IsRequired();
        creatureSnapshotBuilder.Property(x => x.UpdatedAt).IsRequired();
        creatureSnapshotBuilder.HasIndex(x => new { x.BattleId, x.CreatureId }).IsUnique();
        creatureSnapshotBuilder.HasOne(x => x.BattleSession)
            .WithMany(x => x.CreatureSnapshots)
            .HasForeignKey(x => x.BattleId)
            .IsRequired();
    }
}

