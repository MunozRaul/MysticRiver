using DotNet.Testcontainers.Containers;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MysticRiver.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime {
    private TestCtx TestContext { get; set; } = null!;
    private readonly List<IContainer> _containers = [];

    public async ValueTask InitializeAsync() {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();

        services.AddKeyedSingleton(DependenyInjection.ContainerGroupKey, _containers);
        await OnInitializeAsync(services, configuration).ConfigureAwait(false);

        TestContext = new TestCtx(
            services.BuildServiceProvider(),
            configuration
        );

        await Task.WhenAll(_containers.Select(async c =>
            await c.StartAsync().ConfigureAwait(false))
        ).ConfigureAwait(false);

        await AfterInitializeAsync(TestContext).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync() {
        await Task.WhenAll(_containers.Select(async c => {
            await c.StopAsync().ConfigureAwait(false);
            await c.DisposeAsync().ConfigureAwait(false);
        })).ConfigureAwait(false);

        await OnDisposeAsync(TestContext).ConfigureAwait(false);
    }

    public virtual ValueTask OnInitializeAsync(
        IServiceCollection services,
        IConfigurationManager configuration
    ) => ValueTask.CompletedTask;

    public virtual ValueTask AfterInitializeAsync(
        TestCtx ctx
    ) => ValueTask.CompletedTask;

    public virtual ValueTask OnDisposeAsync(
        TestCtx ctx
    ) => ValueTask.CompletedTask;
}
