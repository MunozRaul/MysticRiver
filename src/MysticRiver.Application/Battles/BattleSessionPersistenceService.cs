using Microsoft.EntityFrameworkCore;
using MysticRiver.Application.Data;
using MysticRiver.Application.Data.Entities;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

/// <summary>
/// Persistence service for battle sessions: writes snapshots to PostgreSQL
/// for durability, recovery, and audit.
/// MVP approach: keeps BattleSession in memory, stores metadata + creature snapshots in DB.
/// </summary>
public sealed class BattleSessionPersistenceService {
    private readonly MysticRiverDbContext _dbContext;
    private readonly Func<DateTime> _utcNowProvider;

    public BattleSessionPersistenceService(MysticRiverDbContext dbContext, Func<DateTime>? utcNowProvider = null) {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        _utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
    }

    public async Task SaveSessionSnapshotAsync(BattleSession session) {
        ArgumentNullException.ThrowIfNull(session);

        var now = _utcNowProvider();
        var entity = new BattleSessionEntity {
            BattleId = session.BattleId,
            HostPlayerId = session.HostPlayerId,
            GuestPlayerId = session.GuestPlayerId,
            MatchStatus = (int)session.MatchStatus,
            RoundNumber = session.RoundNumber,
            StateVersion = session.StateVersion,
            EnemyAttackPower = session.EnemyAttackPower,
            CurrentTurnCreatureId = session.CurrentTurnCreatureId,
            ForcedWinnerCreatureId = session.ForcedWinnerCreatureId,
            ForcedEndReason = (int)session.ForcedEndReason,
            CreatedAt = now,
            UpdatedAt = now
        };

        var existing = await _dbContext.BattleSessions.FindAsync(session.BattleId);
        if (existing is not null) {
            existing.HostPlayerId = session.HostPlayerId;
            existing.GuestPlayerId = session.GuestPlayerId;
            existing.MatchStatus = (int)session.MatchStatus;
            existing.RoundNumber = session.RoundNumber;
            existing.StateVersion = session.StateVersion;
            existing.CurrentTurnCreatureId = session.CurrentTurnCreatureId;
            existing.ForcedWinnerCreatureId = session.ForcedWinnerCreatureId;
            existing.ForcedEndReason = (int)session.ForcedEndReason;
            existing.UpdatedAt = now;
        } else {
            _dbContext.BattleSessions.Add(entity);
        }

        await _dbContext.SaveChangesAsync();

        // Also snapshot creature state
        await SaveCreatureSnapshotsAsync(session, now);
    }

    private async Task SaveCreatureSnapshotsAsync(BattleSession session, DateTime now) {
        try {
            var player = session.Battle.Creature1;
            var enemy = session.Battle.Creature2;

            var playerSnapshot = new CreatureSnapshotEntity {
                BattleId = session.BattleId,
                CreatureId = "player",
                Name = player.Name,
                Hp = player.CurrentHp,
                MaxHp = player.MaxHp,
                Mana = player.CurrentMana,
                MaxMana = player.MaxMana,
                Shield = player.CurrentShield,
                StatusEffectsJson = "{}",
                CreatedAt = now,
                UpdatedAt = now
            };

            var enemySnapshot = new CreatureSnapshotEntity {
                BattleId = session.BattleId,
                CreatureId = "enemy",
                Name = enemy.Name,
                Hp = enemy.CurrentHp,
                MaxHp = enemy.MaxHp,
                Mana = enemy.CurrentMana,
                MaxMana = enemy.MaxMana,
                Shield = enemy.CurrentShield,
                StatusEffectsJson = "{}",
                CreatedAt = now,
                UpdatedAt = now
            };

            foreach (var snapshot in new[] { playerSnapshot, enemySnapshot }) {
                var existing = await _dbContext.CreatureSnapshots
                    .FirstOrDefaultAsync<CreatureSnapshotEntity>(x => x.BattleId == snapshot.BattleId && x.CreatureId == snapshot.CreatureId);

                if (existing is not null) {
                    existing.Hp = snapshot.Hp;
                    existing.Mana = snapshot.Mana;
                    existing.Shield = snapshot.Shield;
                    existing.StatusEffectsJson = snapshot.StatusEffectsJson;
                    existing.UpdatedAt = now;
                    _dbContext.CreatureSnapshots.Update(existing);
                } else {
                    _dbContext.CreatureSnapshots.Add(snapshot);
                }
            }

            await _dbContext.SaveChangesAsync();
        } catch (Exception ex) {
            // Non-fatal: creature snapshots are optional for MVP
            Console.WriteLine($"Warning: Failed to save creature snapshots: {ex.Message}");
        }
    }
}
