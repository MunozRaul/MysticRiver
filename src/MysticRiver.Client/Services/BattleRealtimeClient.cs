using Microsoft.AspNetCore.SignalR.Client;

using MysticRiver.Client.Options;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Services;

public sealed class BattleRealtimeClient : IAsyncDisposable {
    private readonly HubConnection _hubConnection;

    public event EventHandler<BattleStateUpdatedEvent>? BattleStateUpdated;

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

    public async ValueTask DisposeAsync() {
        await _hubConnection.DisposeAsync();
    }
}
