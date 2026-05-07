namespace MysticRiver.Client.Options;

public sealed class ClientOptions {
    public const string SectionName = "ClientOptions";

    public string ApiBaseUrl { get; init; } = "https://localhost:7147";
}
