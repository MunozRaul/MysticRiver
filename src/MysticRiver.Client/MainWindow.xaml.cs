using System;
using System.Windows;
using System.Windows.Input;
namespace MysticRiver.Client;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        MenuView.ExitRequested += (_, __) => Application.Current.Shutdown();
        PauseView.AbandonRequested += async (_, __) => {
            try {
                await BattleView.AbandonBattleAsync();
                PauseView.Visibility = Visibility.Collapsed;
                BattleView.Visibility = Visibility.Collapsed;
                MenuView.Visibility = Visibility.Visible;
            }
            catch (Exception exception) {
                MessageBox.Show(exception.Message, "Abandon failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };
        PauseView.ExitRequested += (_, __) => Application.Current.Shutdown();
        PauseView.CancelRequested += (_, __) => PauseView.Visibility = Visibility.Collapsed;
    }

    private async void MenuView_SinglePlayerRequested(object sender, EventArgs e)
    {
        MenuView.Visibility = Visibility.Collapsed;
        BattleView.Visibility = Visibility.Visible;
        await BattleView.InitializeAsync();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Escape)
        {
            return;
        }

        if (PauseView.Visibility == Visibility.Visible)
        {
            PauseView.Visibility = Visibility.Collapsed;
            e.Handled = true;
            return;
        }

        if (BattleView.Visibility == Visibility.Visible)
        {
            // Show pause view overlay
            PauseView.Visibility = Visibility.Visible;
            PauseView.Focus();
            e.Handled = true;
        }
    }
}
