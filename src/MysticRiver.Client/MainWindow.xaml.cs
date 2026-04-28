using System.Windows;

namespace MysticRiver.Client;

public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
        ShowMenu();
    }

    private void MenuView_SinglePlayerRequested(object sender, EventArgs e) {
        ShowBattleScreen();
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
