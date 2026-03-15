using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MysticRiver.TestFramework;

public interface ITestSetup : IAsyncDisposable
{
    public ValueTask ConfigureServicesAsync(
        IServiceCollection services, 
        IConfigurationManager configuration, 
        IList<IContainer> containers
    );
}
