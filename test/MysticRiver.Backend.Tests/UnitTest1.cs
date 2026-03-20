using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MysticRiver.TestFramework;
using Npgsql;

namespace MysticRiver.Backend.Tests;

public class PostgresTests : TestBase<MyTestSetup>
{
    [Fact]
    public async Task Test1()
    {
        var dataSource = Services.GetRequiredService<NpgsqlDataSource>();
        await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT 1";
        await cmd.ExecuteScalarAsync(TestContext.Current.CancellationToken);
    }
}

public sealed class MyTestSetup : ITestSetup
{
    public async ValueTask ConfigureServicesAsync(
        IServiceCollection services, 
        IConfigurationManager configuration,
        IList<IContainer> containers)
    {
        var container = await containers.UsePostgreSqlAsync(builder => builder
            .WithDatabase("cutedb")
            .WithUsername("cutedb")
            .WithPassword("cutedb")
        ).ConfigureAwait(false);

        services.AddNpgsqlDataSource(container.GetConnectionString());
    }

    public ValueTask DisposeAsync()
    {
        return default;
    }
}

