using Microsoft.AspNetCore.SignalR.Client;

using MysticRiver.Client.Options;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Services;

public sealed class BattleRealtimeClient : IAsyncDisposable {
    private readonly HubConnection _hubConnection;
    private string? _joinedBattleId;
    private string? _joinedPlayerId;
    private string? _joinedDisplayName;

    public event EventHandler<BattleStateUpdatedEvent>? BattleStateUpdated;
    public event EventHandler<BattleLifecycleEvent>? BattleLifecycleUpdated;
    public event EventHandler<string>? PlayerTokenRefreshed;
    public event EventHandler? Reconnected;

    public BattleRealtimeClient(ClientOptions clientOptions) {
        ArgumentNullException.ThrowIfNull(clientOptions);

        if (!Uri.TryCreate(clientOptions.ApiBaseUrl, UriKind.Absolute, out var baseUri)) {
            throw new InvalidOperationException($"Invalid configuration value for {ClientOptions.SectionName}:ApiBaseUrl.");
        }

        var hubUrl = new Uri(baseUri, "/hubs/battle");

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();
 
        _hubConnection.On<BattleStateUpdatedEvent>(
            "BattleStateUpdated",
            battleEvent => BattleStateUpdated?.Invoke(this, battleEvent));

        _hubConnection.On<BattleLifecycleEvent>(
            "BattleLifecycleUpdated",
            lifecycleEvent => BattleLifecycleUpdated?.Invoke(this, lifecycleEvent));

        // Forward reconnect notifications so callers can refresh full state if needed
        _hubConnection.Reconnected += async connectionId => {
            await RejoinAndRefreshTokenAsync();
            Reconnected?.Invoke(this, EventArgs.Empty);
        };
    }

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default) {
        if (_hubConnection.State == HubConnectionState.Connected) {
            return;
        }

        await _hubConnection.StartAsync(cancellationToken);
    }

    public async Task<string> JoinBattleAsync(string battleId, string playerId, string displayName, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);
        await EnsureConnectedAsync(cancellationToken);
        var token = await _hubConnection.InvokeAsync<string>("JoinBattle", battleId, playerId, displayName, cancellationToken);
        _joinedBattleId = battleId;
        _joinedPlayerId = playerId;
        _joinedDisplayName = displayName;
        return token;
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if (_hubConnection.State == HubConnectionState.Disconnected) {
            return;
        }

        await _hubConnection.StopAsync(cancellationToken);
        _joinedBattleId = null;
        _joinedPlayerId = null;
        _joinedDisplayName = null;
    }

    public async ValueTask DisposeAsync() {
        await _hubConnection.DisposeAsync();
    }

    private async Task RejoinAndRefreshTokenAsync() {
        if (string.IsNullOrWhiteSpace(_joinedBattleId) || string.IsNullOrWhiteSpace(_joinedPlayerId)) {
            return;
        }

        var token = await _hubConnection.InvokeAsync<string>(
            "JoinBattle",
            _joinedBattleId,
            _joinedPlayerId,
            _joinedDisplayName ?? _joinedPlayerId);
        PlayerTokenRefreshed?.Invoke(this, token);
    }
}
