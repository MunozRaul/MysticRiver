using Microsoft.Extensions.Configuration;

namespace MysticRiver.IntegrationTests;

public sealed record TestCtx(
    IServiceProvider Services,
    IConfigurationManager Configuration
);
