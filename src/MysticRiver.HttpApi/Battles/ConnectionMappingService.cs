using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MysticRiver.HttpApi.Battles;

public sealed class ConnectionMappingService : IConnectionMapping {
    private readonly ConcurrentDictionary<string, (string BattleId, string PlayerId)> _map = new();
    private readonly ConcurrentDictionary<string, (string ConnectionId, string BattleId, string PlayerId)> _tokens = new();

    public void Register(string connectionId, string battleId, string playerId) {
        _map[connectionId] = (battleId, playerId);
    }

    public void Unregister(string connectionId) {
        _map.TryRemove(connectionId, out _);
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

    public IEnumerable<(string ConnectionId, string PlayerId)> GetConnectionsForBattleWithPlayers(string battleId) {
        return _map.Where(kv => kv.Value.BattleId == battleId).Select(kv => (kv.Key, kv.Value.PlayerId));
    }

    public string CreateToken(string connectionId, string battleId, string playerId) {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = (connectionId, battleId, playerId);
        return token;
    }

    public bool TryGetByToken(string token, out string? battleId, out string? playerId) {
        if (_tokens.TryGetValue(token, out var v)) {
            battleId = v.BattleId; playerId = v.PlayerId; return true;
        }
        battleId = null; playerId = null; return false;
    }

    public void RemoveToken(string token) => _tokens.TryRemove(token, out _);
}
