using AutoUpdaterDotNET;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace MysticRiver.Client
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var updater = new UpdateService();
            updater.CheckForUpdates();

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
