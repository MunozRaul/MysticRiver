using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace MysticRiver.TestFramework;

public abstract class TestBase<TSetup> : IAsyncLifetime where TSetup : class, ITestSetup, new()
{
    protected IServiceProvider Services { get; private set; } = null!;
    protected IConfigurationManager Configuration { get; private set; } = null!;
    protected IList<IContainer> Containers { get; } = new List<IContainer>();
    
    private readonly TSetup _setup = new();
    
    public async ValueTask InitializeAsync()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationManager();
        
        // TODO: configure default testing services and logging
        await _setup.ConfigureServicesAsync(services, configuration, Containers).ConfigureAwait(false);
        
        Services = services.BuildServiceProvider();
        Configuration = configuration;
    }
    
    public async ValueTask DisposeAsync()
    {
        await _setup.DisposeAsync().ConfigureAwait(false);
        foreach (var container in Containers)
        {
            await container.DisposeAsync().ConfigureAwait(false);
        }
    }
}