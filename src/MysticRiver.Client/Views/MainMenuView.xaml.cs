using System.Windows;
using System.Windows.Controls;

namespace MysticRiver.Client.Views;

public partial class MainMenuView : UserControl {
    public event EventHandler? SinglePlayerRequested;

    public MainMenuView() {
        InitializeComponent();
    }

    private void SinglePlayerButton_Click(object sender, RoutedEventArgs e) {
        SinglePlayerRequested?.Invoke(this, EventArgs.Empty);
    }
}
