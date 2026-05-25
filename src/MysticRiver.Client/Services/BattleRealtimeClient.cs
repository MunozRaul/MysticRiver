using Microsoft.AspNetCore.SignalR.Client;

using MysticRiver.Client.Options;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Services;

public sealed class BattleRealtimeClient : IAsyncDisposable {
    private readonly HubConnection _hubConnection;

    public event EventHandler<BattleStateUpdatedEvent>? BattleStateUpdated;
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

        // Forward reconnect notifications so callers can refresh full state if needed
        _hubConnection.Reconnected += connectionId => {
            Reconnected?.Invoke(this, EventArgs.Empty);
            return Task.CompletedTask;
        };
    }

    public async Task EnsureConnectedAsync(CancellationToken cancellationToken = default) {
        if (_hubConnection.State == HubConnectionState.Connected) {
            return;
        }

        await _hubConnection.StartAsync(cancellationToken);
    }

    public async Task JoinBattleAsync(string battleId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        await EnsureConnectedAsync(cancellationToken);
        await _hubConnection.InvokeAsync("JoinBattle", battleId, cancellationToken);
    }

    public async Task DisconnectAsync(CancellationToken cancellationToken = default) {
        if (_hubConnection.State == HubConnectionState.Disconnected) {
            return;
        }

        await _hubConnection.StopAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync() {
        await _hubConnection.DisposeAsync();
    }
}
