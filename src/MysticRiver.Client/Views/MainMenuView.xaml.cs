using System;
using System.Windows;
using System.Windows.Controls;

namespace MysticRiver.Client.Views;

public sealed class MultiplayerJoinRequestedEventArgs(string battleId) : EventArgs {
    public string BattleId { get; } = battleId;
}

public partial class MainMenuView : UserControl {
    public event EventHandler? SinglePlayerRequested;
    public event EventHandler? MultiplayerHostRequested;
    public event EventHandler<MultiplayerJoinRequestedEventArgs>? MultiplayerJoinRequested;
    public event EventHandler? WaitingCancelled;
    public event EventHandler? ExitRequested;

    public MainMenuView() {
        InitializeComponent();
    }

    private void SinglePlayerButton_Click(object sender, RoutedEventArgs e) {
        SinglePlayerRequested?.Invoke(this, EventArgs.Empty);
    }

    private void HostMultiplayerButton_Click(object sender, RoutedEventArgs e) {
        MultiplayerHostRequested?.Invoke(this, EventArgs.Empty);
    }

    private void JoinMultiplayerButton_Click(object sender, RoutedEventArgs e) {
        var battleId = JoinBattleIdTextBox.Text?.Trim();
        if (string.IsNullOrWhiteSpace(battleId)) {
            MessageBox.Show("Please enter a battle ID to join.", "Missing Battle ID", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        MultiplayerJoinRequested?.Invoke(this, new MultiplayerJoinRequestedEventArgs(battleId));
    }

    private void CancelWaitingButton_Click(object sender, RoutedEventArgs e) {
        WaitingCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }

    public void ShowModeSelection() {
        ModeSelectionPanel.Visibility = Visibility.Visible;
        WaitingRoomPanel.Visibility = Visibility.Collapsed;
    }

    public void ShowWaitingRoom(string battleId, string hostName, string guestName, string matchStatus, string connectionStatus) {
        WaitingBattleIdTextBlock.Text = $"Battle ID: {battleId}";
        WaitingHostTextBlock.Text = $"Host: {hostName}";
        WaitingGuestTextBlock.Text = $"Guest: {guestName}";
        WaitingStatusTextBlock.Text = $"Match status: {matchStatus}";
        WaitingConnectionTextBlock.Text = $"Connection: {connectionStatus}";

        ModeSelectionPanel.Visibility = Visibility.Collapsed;
        WaitingRoomPanel.Visibility = Visibility.Visible;
    }

    public void UpdateWaitingRoom(string hostName, string guestName, string matchStatus, string connectionStatus) {
        WaitingHostTextBlock.Text = $"Host: {hostName}";
        WaitingGuestTextBlock.Text = $"Guest: {guestName}";
        WaitingStatusTextBlock.Text = $"Match status: {matchStatus}";
        WaitingConnectionTextBlock.Text = $"Connection: {connectionStatus}";
    }
}
