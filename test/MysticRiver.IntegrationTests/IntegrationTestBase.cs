using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MysticRiver.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private TestContext TestContext { get; set; } = null!;
    private List<IContainer> containers = new List<IContainer>();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();

        services.AddKeyedSingleton(DependenyInjection.ContainerGroupKey, containers);
        await OnInitializeAsync(services, configuration).ConfigureAwait(false);

        TestContext = new TestContext(
            services.BuildServiceProvider(),
            configuration
        );

        await Task.WhenAll(containers.Select(async c =>
            await c.StartAsync().ConfigureAwait(false))
        ).ConfigureAwait(false);

        await AfterInitializeAsync(TestContext).ConfigureAwait(false);
    }

    public async Task DisposeAsync()
    {
        await Task.WhenAll(containers.Select(async c =>
        {
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
        TestContext ctx
    ) => ValueTask.CompletedTask;

    public virtual ValueTask OnDisposeAsync(
        TestContext ctx
    ) => ValueTask.CompletedTask;
}