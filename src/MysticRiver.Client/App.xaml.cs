using System.Windows;

<<<<<<< HEAD
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
    protected override async void OnStartup(StartupEventArgs e) {
        base.OnStartup(e);

        await _host.StartAsync();
        await _host.StartAsync();

        var updater = _host.Services.GetRequiredService<UpdateService>();
        updater.CheckForUpdates();
=======
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

>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e
        var updater = _host.Services.GetRequiredService<UpdateService>();
        updater.CheckForUpdates();

        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
<<<<<<< HEAD
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }
=======
>>>>>>> 43c789db65e4c0cd13f0450389424740910b442e

    protected override async void OnExit(ExitEventArgs e) {
        await _host.StopAsync();
        _host.Dispose();
        base.OnExit(e);
    }
}
