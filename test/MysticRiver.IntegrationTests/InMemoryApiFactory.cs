using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MysticRiver.Application.Data;

namespace MysticRiver.IntegrationTests;

/// <summary>
/// WebApplicationFactory that replaces PostgreSQL with an in-memory database
/// so API integration tests don't require a running Postgres instance.
/// </summary>
public sealed class InMemoryApiFactory : WebApplicationFactory<Program> {
    protected override void ConfigureWebHost(IWebHostBuilder builder) {
        builder.ConfigureServices(services => {
            // Remove the real PostgreSQL DbContext registration
            services.RemoveAll<DbContextOptions<MysticRiverDbContext>>();
            services.RemoveAll<MysticRiverDbContext>();

            // Use a dedicated internal service provider to avoid the
            // "multiple database providers registered" conflict with Npgsql.
            var internalServiceProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<MysticRiverDbContext>(options => {
                options.UseInMemoryDatabase("test-" + Guid.NewGuid());
                options.UseInternalServiceProvider(internalServiceProvider);
            });
        });
    }
}
