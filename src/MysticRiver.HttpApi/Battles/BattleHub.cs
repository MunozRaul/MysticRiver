using Microsoft.AspNetCore.SignalR;

namespace MysticRiver.HttpApi.Battles;

public sealed class BattleHub : Hub<IBattleClient> {
    private readonly IConnectionMapping _connectionMapping;

    public BattleHub(IConnectionMapping connectionMapping) {
        _connectionMapping = connectionMapping;
    }

    public Task JoinBattle(string battleId, string playerId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);

        _connectionMapping.Register(Context.ConnectionId, battleId, playerId);
        return Groups.AddToGroupAsync(Context.ConnectionId, battleId);
    }

    public override Task OnDisconnectedAsync(Exception? exception) {
        _connectionMapping.Unregister(Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
