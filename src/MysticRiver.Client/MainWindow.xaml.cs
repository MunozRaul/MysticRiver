using System.Net.Http;
using System.Windows;

using Microsoft.AspNetCore.SignalR;

namespace MysticRiver.Client;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        ShowMenu();
    }

    private async void MenuView_SinglePlayerRequested(object sender, EventArgs e) {
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(e);

        ShowBattleScreen();

        try {
            await BattleView.InitializeAsync();
        }
        catch (HttpRequestException exception) {
            MessageBox.Show(
                this,
                $"Could not connect to the HTTP API.\n{exception.Message}",
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (HubException exception) {
            MessageBox.Show(
                this,
                $"Could not join live battle updates.\n{exception.Message}",
                "SignalR Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (InvalidOperationException exception) {
            MessageBox.Show(
                this,
                $"Battle initialization failed.\n{exception.Message}",
                "Initialization Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void ShowMenu() {
        MenuView.Visibility = Visibility.Visible;
        BattleView.Visibility = Visibility.Collapsed;
    }

    private void ShowBattleScreen() {
        MenuView.Visibility = Visibility.Collapsed;
        BattleView.Visibility = Visibility.Visible;
    }
}
