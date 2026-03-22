using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace MysticRiver.IntegrationTests;

internal static class DependenyInjection
{
    // when registering a container with one of the extension
    // methods we do not want to make the user keep track of all containers.
    // thats why we put them into an IEnumerable and dispose of them ourselves
    public const string ContainerGroupKey = "TestContainers";

    extension(IServiceCollection services)
    {
        public IServiceCollection AddPostgreSqlContainer(
            string image,
            Action<PostgreSqlBuilder> setup,
            object? key = null
        )
        {
            var builder = new PostgreSqlBuilder(image);
            setup(builder);

            var container = builder.Build();
            if (key is null)
            {
                services.AddSingleton(container);
            }
            else
            {
                services.AddKeyedSingleton(container, key);
            }

            var containerList = services.BuildServiceProvider().GetRequiredKeyedService<List<IContainer>>(ContainerGroupKey);
            containerList.Add(container);
            return services;
        }
    }
}