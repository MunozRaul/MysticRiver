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
        // Wire menu exit and pause view events
        PauseView.Loaded += (s, e) => {
            if (PauseView is Views.PauseView pv) {
                pv.AbandonRequested += async (_, __) => {
                    try { await BattleView.CleanupAsync(); } catch { }
                    PauseView.Visibility = Visibility.Collapsed;
                    BattleView.Visibility = Visibility.Collapsed;
                    MenuView.Visibility = Visibility.Visible;
                };
                pv.ExitRequested += (_, __) => Application.Current.Shutdown();
                pv.CancelRequested += (_, __) => { PauseView.Visibility = Visibility.Collapsed; };
            }
        };
        MenuView.Loaded += (s, e) => {
            // subscribe to exit if available (MainMenuView will expose it)
            if (MenuView is Views.MainMenuView mm) {
                mm.ExitRequested += (_, __) => Application.Current.Shutdown();
            }
        };
    }

    private async void MenuView_SinglePlayerRequested(object sender, EventArgs e)
    {
        MenuView.Visibility = Visibility.Collapsed;
        BattleView.Visibility = Visibility.Visible;
        await BattleView.InitializeAsync();
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // Only act when battle is visible
        if (e.Key == Key.Escape && BattleView.Visibility == Visibility.Visible)
        {
            // Show pause view overlay
            PauseView.Visibility = Visibility.Visible;
            e.Handled = true;
        }
    }
}
