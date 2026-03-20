using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MysticRiver.IntegrationTests;
using Npgsql;
using Testcontainers.PostgreSql;

// TODO: discuss in Team if `IntegrationTestBase` or idiomatic IClassFixture<TFixture> ?

public class PostgresTests : IntegrationTestBase
{
    [Fact]
    public void Test1()
    {
        var sql = TestContext.Containers[typeof(PostgreSqlContainer)] as PostgreSqlContainer;
        var connStr = sql!.GetConnectionString();

        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT 1", conn);
        var result = cmd.ExecuteScalar();

        Assert.Equal(1, Convert.ToInt32(result));
    }

    public override async ValueTask OnInitializeAsync(
        IServiceCollection services,
        IConfigurationManager configuration,
        Dictionary<Type, IContainer> containers)
    {
        var postgresSql = new PostgreSqlBuilder("postgres:18.3")
            .WithDatabase("mysticriver")
            .WithUsername("mysticriver")
            .WithPassword("mysticriver")
            .Build();

        containers[typeof(PostgreSqlContainer)] = postgresSql;
        await postgresSql.StartAsync();
    }

    public override async ValueTask OnDisposeAsync(TestContext ctx)
    {
        if (ctx.Containers[typeof(PostgreSqlContainer)] is PostgreSqlContainer sql)
            await sql.StopAsync();
    }
}