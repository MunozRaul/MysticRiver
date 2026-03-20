using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MysticRiver.IntegrationTests;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected TestContext TestContext { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
        var containers = new Dictionary<Type, IContainer>();

        await OnInitializeAsync(services, configuration, containers);

        TestContext = new TestContext(
            services.BuildServiceProvider(),
            configuration,
            containers
        );
    }

    public async Task DisposeAsync()
    {
        await OnDisposeAsync(TestContext);
    }

    public virtual ValueTask OnInitializeAsync(
        IServiceCollection services,
        IConfigurationManager configuration,
        Dictionary<Type, IContainer> containers
    ) => ValueTask.CompletedTask;

    public virtual ValueTask OnDisposeAsync(TestContext ctx) => ValueTask.CompletedTask;
}