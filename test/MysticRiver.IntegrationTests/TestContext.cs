using DotNet.Testcontainers.Containers;
using Microsoft.Extensions.Configuration;

namespace MysticRiver.IntegrationTests;

public sealed record TestContext(
    IServiceProvider Services,
    IConfigurationManager Configuration,
    Dictionary<Type, IContainer> Containers // TODO: remove and instead register into service provider
);
