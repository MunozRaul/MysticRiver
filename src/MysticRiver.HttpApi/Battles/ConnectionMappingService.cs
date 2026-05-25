using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MysticRiver.HttpApi.Battles;

public sealed class ConnectionMappingService : IConnectionMapping {
    private readonly ConcurrentDictionary<string, (string BattleId, string PlayerId, string? DisplayName)> _map = new();

    // token -> (connectionId, battleId, playerId, displayName, createdAt, singleUse)
    private readonly ConcurrentDictionary<string, (string ConnectionId, string BattleId, string PlayerId, string? DisplayName, DateTimeOffset CreatedAt, bool SingleUse)> _tokens = new();

    private static readonly TimeSpan TokenTtl = TimeSpan.FromMinutes(15);

    public void Register(string connectionId, string battleId, string playerId, string? displayName = null) {
        _map[connectionId] = (battleId, playerId, displayName);
    }

    public void Unregister(string connectionId) {
        _map.TryRemove(connectionId, out _);
        // Remove any tokens associated with this connection
        var keys = _tokens.Where(kv => kv.Value.ConnectionId == connectionId).Select(kv => kv.Key).ToList();
        foreach (var k in keys) { _tokens.TryRemove(k, out _); }
    }

    public bool TryGetPlayer(string connectionId, out string? playerId) {
        if (_map.TryGetValue(connectionId, out var v)) { playerId = v.PlayerId; return true; }
        playerId = null; return false;
    }

    public IEnumerable<string> GetConnectionsForBattle(string battleId) {
        return _map.Where(kv => kv.Value.BattleId == battleId).Select(kv => kv.Key);
    }

    public IEnumerable<(string ConnectionId, string PlayerId, string? DisplayName)> GetConnectionsForBattleWithPlayers(string battleId) {
        return _map.Where(kv => kv.Value.BattleId == battleId).Select(kv => (kv.Key, kv.Value.PlayerId, kv.Value.DisplayName));
    }

    public string CreateToken(string connectionId, string battleId, string playerId, string? displayName = null, bool singleUse = false) {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = (connectionId, battleId, playerId, displayName, DateTimeOffset.UtcNow, singleUse);
        return token;
    }

    public bool TryGetByToken(string token, out string? battleId, out string? playerId, out string? displayName) {
        battleId = null; playerId = null; displayName = null;
        if (!_tokens.TryGetValue(token, out var v)) { return false; }

        // Check expiry
        if (DateTimeOffset.UtcNow - v.CreatedAt > TokenTtl) {
            // expired
            _tokens.TryRemove(token, out _);
            return false;
        }

        battleId = v.BattleId; playerId = v.PlayerId; displayName = v.DisplayName;

        if (v.SingleUse) {
            _tokens.TryRemove(token, out _);
        }

        return true;
    }

    public void RemoveToken(string token) => _tokens.TryRemove(token, out _);

    public void RemoveExpiredTokens() {
        var now = DateTimeOffset.UtcNow;
        var expired = _tokens.Where(kv => now - kv.Value.CreatedAt > TokenTtl).Select(kv => kv.Key).ToList();
        foreach (var k in expired) {
            _tokens.TryRemove(k, out _);
        }
    }
}
