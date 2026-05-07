using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

using MysticRiver.Client.Options;
using MysticRiver.Client.Services;

namespace MysticRiver.Client;

public partial class App : Application {
    private readonly IHost _host;
    public static IServiceProvider Services { get; private set; } = null!;

    public App() {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) => {
                services.AddOptions<ClientOptions>()
                    .Bind(context.Configuration.GetSection(ClientOptions.SectionName));
                services.AddSingleton(provider => provider.GetRequiredService<IOptions<ClientOptions>>().Value);

                services.AddHttpClient<BattleApiClient>((provider, client) => {
                    var clientOptions = provider.GetRequiredService<ClientOptions>();
                    client.BaseAddress = ResolveApiBaseUri(clientOptions);
                });
                services.AddSingleton<BattleRealtimeClient>();
                services.AddSingleton<UpdateService>();
                services.AddSingleton<MainWindow>();
            })
            .Build();

        Services = _host.Services;
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

    private static Uri ResolveApiBaseUri(ClientOptions clientOptions) {
        ArgumentNullException.ThrowIfNull(clientOptions);

        if (!Uri.TryCreate(clientOptions.ApiBaseUrl, UriKind.Absolute, out var uri)) {
            throw new InvalidOperationException($"Invalid configuration value for {ClientOptions.SectionName}:ApiBaseUrl.");
        }

        return uri;
    }
}
