using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MysticRiver.HttpApi.Battles;

public sealed class ConnectionMappingService : IConnectionMapping {
    private readonly ConcurrentDictionary<string, (string BattleId, string PlayerId)> _map = new();

    public void Register(string connectionId, string battleId, string playerId) {
        _map[connectionId] = (battleId, playerId);
    }

    public void Unregister(string connectionId) => _map.TryRemove(connectionId, out _);

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
}
