using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MysticRiver.Client;

public partial class App : Application {
    private readonly IHost _host;

    public App() {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => {
                services.AddSingleton<UpdateService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();
    }

    protected override async void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        await _host.StartAsync();

        var updater = _host.Services.GetRequiredService<UpdateService>();
        updater.CheckForUpdates();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    protected override async void OnExit(ExitEventArgs e) {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
