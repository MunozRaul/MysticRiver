using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using MysticRiver.Client.Options;
using MysticRiver.Client.Services;

namespace MysticRiver.Client;

public partial class App : Application {
    private readonly IHost _host;

    public App() {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) => {
                services.AddSingleton<UpdateService>();
                services.AddSingleton<MainWindow>();
                services.AddOptions<ClientOptions>()
                    .Bind(context.Configuration.GetSection(ClientOptions.SectionName));
                services.AddSingleton(sp => sp.GetRequiredService<IOptions<ClientOptions>>().Value);
                services.AddHttpClient<BattleApiClient>((sp, client) => {
                    var options = sp.GetRequiredService<ClientOptions>();
                    client.BaseAddress = new Uri(options.ApiBaseUrl);
                });
                services.AddSingleton<BattleRealtimeClient>();
            })
            .Build();
    }

    public static IServiceProvider Services {
        get {
            if (Current is not App app) {
                throw new InvalidOperationException("The application host is not initialized.");
            }

            return app._host.Services;
        }
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
