using System.Collections.Generic;

namespace MysticRiver.HttpApi.Battles;

public interface IConnectionMapping {
    void Register(string connectionId, string battleId, string playerId);
    void Unregister(string connectionId);
    bool TryGetPlayer(string connectionId, out string? playerId);
    IEnumerable<string> GetConnectionsForBattle(string battleId);
    IEnumerable<(string ConnectionId, string PlayerId)> GetConnectionsForBattleWithPlayers(string battleId);

    // Token-based ephemeral authentication for HTTP action calls
    string CreateToken(string connectionId, string battleId, string playerId);
    bool TryGetByToken(string token, out string? battleId, out string? playerId);
    void RemoveToken(string token);
}
