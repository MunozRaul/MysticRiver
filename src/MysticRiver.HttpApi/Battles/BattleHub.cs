using Microsoft.AspNetCore.SignalR;
using MysticRiver.Application.Battles;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.HttpApi.Battles;

public sealed class BattleHub : Hub<IBattleClient> {
    private readonly IConnectionMapping _connectionMapping;
    private readonly IBattleService _battleService;

    public BattleHub(IConnectionMapping connectionMapping, IBattleService battleService) {
        _connectionMapping = connectionMapping;
        _battleService = battleService;
    }

    // Returns an ephemeral token the client should use for subsequent HTTP calls
    public async Task<string> JoinBattle(string battleId, string playerId, string? displayName = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);
        _battleService.ValidateRealtimeJoin(battleId, playerId);

        await Groups.AddToGroupAsync(Context.ConnectionId, battleId);
        _connectionMapping.Register(Context.ConnectionId, battleId, playerId, displayName);
        var token = _connectionMapping.CreateToken(Context.ConnectionId, battleId, playerId, displayName);

        var joinEvent = new BattleLifecycleEvent(
            battleId,
            BattleLifecycleEventKind.OpponentJoined,
            playerId,
            displayName);
        await Clients.OthersInGroup(battleId).BattleLifecycleUpdated(joinEvent);

        var state = _battleService.GetBattleState(battleId);
        if (state.MatchStatus == MatchStatus.Ready || state.MatchStatus == MatchStatus.InProgress) {
            await Clients.Group(battleId).BattleLifecycleUpdated(new BattleLifecycleEvent(
                battleId,
                BattleLifecycleEventKind.BattleStarted));
        }

        return token;
    }

    public override async Task OnDisconnectedAsync(Exception? exception) {
        if (_connectionMapping.TryGetRegistration(Context.ConnectionId, out var battleId, out var playerId, out var displayName)
            && !string.IsNullOrWhiteSpace(battleId)) {
            var disconnectEvent = new BattleLifecycleEvent(
                battleId!,
                BattleLifecycleEventKind.OpponentDisconnected,
                playerId,
                displayName,
                BattleEndReason.Disconnect);
            await Clients.OthersInGroup(battleId!).BattleLifecycleUpdated(disconnectEvent);
        }

        _connectionMapping.Unregister(Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }
}
