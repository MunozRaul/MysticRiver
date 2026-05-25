using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using MysticRiver.Client.Services;
using MysticRiver.Client.Views;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client;

public partial class MainWindow : Window {
    private readonly BattleApiClient _battleApiClient;
    private readonly GuestIdentityService _guestIdentityService;
    private CancellationTokenSource? waitingCts;

    public MainWindow() {
        InitializeComponent();
        _battleApiClient = App.Services.GetRequiredService<BattleApiClient>();
        _guestIdentityService = App.Services.GetRequiredService<GuestIdentityService>();

        MenuView.SinglePlayerRequested += MenuView_SinglePlayerRequested;
        MenuView.MultiplayerHostRequested += MenuView_MultiplayerHostRequested;
        MenuView.MultiplayerJoinRequested += MenuView_MultiplayerJoinRequested;
        MenuView.WaitingCancelled += MenuView_WaitingCancelled;
        MenuView.ExitRequested += (_, __) => Application.Current.Shutdown();

        PauseView.AbandonRequested += async (_, __) => {
            try {
                await BattleView.AbandonBattleAsync();
                PauseView.Visibility = Visibility.Collapsed;
                BattleView.Visibility = Visibility.Collapsed;
                MenuView.Visibility = Visibility.Visible;
                MenuView.ShowModeSelection();
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message, "Abandon failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        PauseView.ExitRequested += (_, __) => Application.Current.Shutdown();
        PauseView.CancelRequested += (_, __) => PauseView.Visibility = Visibility.Collapsed;
    }

    private async void MenuView_SinglePlayerRequested(object? sender, EventArgs e) {
        try {
            MenuView.Visibility = Visibility.Collapsed;
            BattleView.Visibility = Visibility.Visible;
            await BattleView.InitializeAsync();
        }
        catch (Exception exception) {
            BattleView.Visibility = Visibility.Collapsed;
            MenuView.Visibility = Visibility.Visible;
            MenuView.ShowModeSelection();
            MessageBox.Show(exception.Message, "Single player startup failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MenuView_MultiplayerHostRequested(object? sender, EventArgs e) {
        try {
            waitingCts?.Cancel();
            waitingCts = new CancellationTokenSource();

            var identity = _guestIdentityService.GetOrCreateIdentity();
            var created = await _battleApiClient.CreateMatchAsync(new CreateMatchRequest(
                HostPlayerId: identity.PlayerId,
                HostDisplayName: identity.DisplayName,
                OpponentDisplayName: "Waiting for opponent"));

            MenuView.ShowWaitingRoom(
                created.BattleId,
                created.State.Creature1.Name,
                created.State.Creature2.Name,
                created.MatchStatus.ToString(),
                "Waiting for guest...");

            _ = WaitForOpponentAndStartAsync(created.BattleId, identity.PlayerId, identity.DisplayName, created.HostCreatureId, waitingCts.Token);
        }
        catch (Exception exception) {
            MessageBox.Show(exception.Message, "Host setup failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void MenuView_MultiplayerJoinRequested(object? sender, MultiplayerJoinRequestedEventArgs e) {
        try {
            var identity = _guestIdentityService.GetOrCreateIdentity();
            var joined = await _battleApiClient.JoinMatchAsync(
                e.BattleId,
                new JoinMatchRequest(identity.PlayerId, identity.DisplayName));

            await StartMultiplayerBattleAsync(joined.BattleId, joined.State, joined.GuestCreatureId, identity.PlayerId, identity.DisplayName);
        }
        catch (Exception exception) {
            MessageBox.Show(exception.Message, "Join failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MenuView_WaitingCancelled(object? sender, EventArgs e) {
        waitingCts?.Cancel();
        MenuView.ShowModeSelection();
    }

    private async Task WaitForOpponentAndStartAsync(
        string battleId,
        string localPlayerId,
        string localDisplayName,
        string localCreatureId,
        CancellationToken cancellationToken) {
        while (!cancellationToken.IsCancellationRequested) {
            var state = await _battleApiClient.GetBattleStateAsync(battleId, cancellationToken);
            MenuView.UpdateWaitingRoom(
                state.Creature1.Name,
                state.Creature2.Name,
                state.MatchStatus.ToString(),
                "Connected");

            if (state.MatchStatus is MatchStatus.Ready or MatchStatus.InProgress) {
                await StartMultiplayerBattleAsync(battleId, state, localCreatureId, localPlayerId, localDisplayName);
                return;
            }

            await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken);
        }
    }

    private async Task StartMultiplayerBattleAsync(
        string battleId,
        BattleStateDto state,
        string localCreatureId,
        string localPlayerId,
        string localDisplayName) {
        waitingCts?.Cancel();
        MenuView.Visibility = Visibility.Collapsed;
        BattleView.Visibility = Visibility.Visible;
        PauseView.Visibility = Visibility.Collapsed;

        await BattleView.InitializeMultiplayerAsync(battleId, state, localCreatureId, localPlayerId, localDisplayName);
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e) {
        if (e.Key != Key.Escape) {
            return;
        }

        if (PauseView.Visibility == Visibility.Visible) {
            PauseView.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }

        if (BattleView.Visibility == Visibility.Visible) {
            PauseView.Visibility = Visibility.Visible;
            PauseView.Focus();
            e.Handled = true;
        }
    }
}
