using System.Collections.Concurrent;

using MysticRiver.Contracts.Battle;
using MysticRiver.Domain;

namespace MysticRiver.Application.Battles;

/// <summary>
/// Session metadata with creation time for TTL-based cleanup.
/// </summary>
internal sealed record SessionMetadata(BattleSession Session, DateTime CreatedAtUtc);

public sealed class InMemoryBattleSessionStore : IBattleSessionStore {
    /// <summary>
    /// Default session TTL: 1 hour. Override via constructor for testing.
    /// </summary>
    private static readonly TimeSpan DefaultSessionTtl = TimeSpan.FromHours(1);

    private readonly ConcurrentDictionary<string, SessionMetadata> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly TimeSpan _sessionTtl;
    private readonly Func<DateTime> _utcNowProvider;

    public InMemoryBattleSessionStore(TimeSpan? sessionTtl = null, Func<DateTime>? utcNowProvider = null) {
        _sessionTtl = sessionTtl ?? DefaultSessionTtl;
        _utcNowProvider = utcNowProvider ?? (() => DateTime.UtcNow);
    }

    public BattleSession Create(StartBattleRequest request) {
        ArgumentNullException.ThrowIfNull(request);

        CleanupExpiredSessions();

        var battleId = Guid.NewGuid().ToString("N");
        var player = new Creature(request.PlayerName, request.PlayerMaxHp, request.PlayerInitiative);
        var enemy = new Creature(request.EnemyName, request.EnemyMaxHp, request.EnemyInitiative);
        var battle = new Battle(player, enemy);
        var session = new BattleSession(battleId, battle, request.EnemyAttackPower);
        var metadata = new SessionMetadata(session, _utcNowProvider());

        if (_sessions.TryAdd(battleId, metadata)) {
            return session;
        }

        throw new InvalidOperationException("Failed to create a new battle session.");
    }

    public bool TryGet(string battleId, out BattleSession session) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        
        CleanupExpiredSessions();

        if (_sessions.TryGetValue(battleId, out var metadata)) {
            session = metadata.Session;
            return true;
        }

        session = null!;
        return false;
    }

    /// <summary>
    /// Removes sessions that have exceeded their TTL.
    /// </summary>
    private void CleanupExpiredSessions() {
        var now = _utcNowProvider();
        var expiredKeys = _sessions
            .Where(kvp => now - kvp.Value.CreatedAtUtc > _sessionTtl)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys) {
            _sessions.TryRemove(key, out _);
        }
    }
}
