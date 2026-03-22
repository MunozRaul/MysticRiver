using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;

namespace MysticRiver.IntegrationTests;

public sealed record TestContext(
    IServiceProvider Services,
    IConfigurationManager Configuration
);
