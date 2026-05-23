using System.Windows;
using System.Windows.Controls;

namespace MysticRiver.Client.Views;

public partial class MainMenuView : UserControl {
    public event EventHandler? SinglePlayerRequested;
    public event EventHandler? ExitRequested;

    public MainMenuView() {
        InitializeComponent();
    }

    private void SinglePlayerButton_Click(object sender, RoutedEventArgs e) {
        SinglePlayerRequested?.Invoke(this, EventArgs.Empty);
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) {
        ExitRequested?.Invoke(this, EventArgs.Empty);
    }
}
