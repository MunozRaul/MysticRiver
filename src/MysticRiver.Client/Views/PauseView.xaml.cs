using System;
using System.Windows;
using System.Windows.Controls;

namespace MysticRiver.Client.Views;

public partial class PauseView : UserControl {
    public event EventHandler? AbandonRequested;

    public event EventHandler? ExitRequested;
    public event EventHandler? CancelRequested;

    public PauseView() {
        InitializeComponent();
        AbandonButton.Click += (s, e) => AbandonRequested?.Invoke(this, EventArgs.Empty);
        ExitButton.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        CancelButton.Click += (s, e) => CancelRequested?.Invoke(this, EventArgs.Empty);
    }

    private void PauseView_Loaded(object sender, RoutedEventArgs e) {
        Focus();
    }

    private void PauseView_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e) {
        if (e.Key == System.Windows.Input.Key.Escape) {
            CancelRequested?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }
    }
}
