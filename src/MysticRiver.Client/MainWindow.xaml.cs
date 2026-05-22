using System;
using System.Windows;
namespace MysticRiver.Client;
/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window {
    public MainWindow() {
        InitializeComponent();
    }

    private async void MenuView_SinglePlayerRequested(object sender, EventArgs e)
    {
        MenuView.Visibility = Visibility.Collapsed;
        BattleView.Visibility = Visibility.Visible;
        await BattleView.InitializeAsync();
    }
}
