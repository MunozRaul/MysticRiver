using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MysticRiver.IntegrationTests;

public class PostgresTests : IntegrationTestBase
{
    private PostgreSqlContainer postgreSql = null!;

    [Fact]
    public void Test1()
    {
        var connStr = postgreSql.GetConnectionString();

        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT 1", conn);
        var result = cmd.ExecuteScalar();

        Assert.Equal(1, Convert.ToInt32(result));
    }

    public override ValueTask OnInitializeAsync(
        IServiceCollection services,
        IConfigurationManager configuration)
    {
        services.AddPostgreSqlContainer("postgres:18.3", builder => builder
            .WithDatabase("mysticriver")
            .WithUsername("mysticriver")
            .WithPassword("mysticriver")
        );

        return ValueTask.CompletedTask;
    }

    public override ValueTask AfterInitializeAsync(TestCtx ctx)
    {
        postgreSql = ctx.Services.GetRequiredService<PostgreSqlContainer>();
        return ValueTask.CompletedTask;
    }
}
