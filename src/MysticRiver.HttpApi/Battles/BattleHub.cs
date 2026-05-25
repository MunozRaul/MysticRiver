using Microsoft.AspNetCore.SignalR;

namespace MysticRiver.HttpApi.Battles;

public sealed class BattleHub : Hub<IBattleClient> {
    private readonly IConnectionMapping _connectionMapping;

    public BattleHub(IConnectionMapping connectionMapping) {
        _connectionMapping = connectionMapping;
    }

    // Returns an ephemeral token the client should use for subsequent HTTP calls
    public Task<string> JoinBattle(string battleId, string playerId, string? displayName = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);

        _connectionMapping.Register(Context.ConnectionId, battleId, playerId, displayName);
        var token = _connectionMapping.CreateToken(Context.ConnectionId, battleId, playerId, displayName);
        return Task.FromResult(token);
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        _connectionMapping.Unregister(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
