using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MysticRiver.TestFramework;

public static class TestContainerExtensions
{
    private const string PostgreSqlImage = "postgres:18.3";
    
    public static async Task<PostgreSqlContainer> UsePostgreSqlAsync(
        this IList<IContainer> containers,
        Action<PostgreSqlBuilder> configureSqlServerBuilder)
    {
        var builder = new PostgreSqlBuilder(PostgreSqlImage);
        configureSqlServerBuilder(builder);
        
        var container = builder.Build();
        containers.Add(container);
        await container.StartAsync().ConfigureAwait(false);
        return container;
    }
}
