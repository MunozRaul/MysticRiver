using System.Net.Http;
using System.Net.Http.Json;

using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Services;

public sealed class BattleApiClient(HttpClient httpClient)
{
    private readonly HttpClient _httpClient = httpClient;

    public async Task<StartBattleResponse> StartBattleAsync(StartBattleRequest? request = null, CancellationToken cancellationToken = default)
    {
        var payload = request ?? new StartBattleRequest();
        using var response = await _httpClient.PostAsJsonAsync("api/battles/start", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StartBattleResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Battle start response was empty.");
    }

    public async Task<BattleStateDto> ExecuteBasicAttackAsync(
        string battleId,
        ExecuteBasicAttackRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

        using var response = await _httpClient.PostAsJsonAsync(
            $"api/battles/{battleId}/actions/basic-attack",
            request,
            cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Battle state response was empty.");
    }
}
