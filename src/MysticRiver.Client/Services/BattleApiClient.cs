using System.Net.Http;
using System.Net.Http.Json;

using MysticRiver.Contracts.Battle;

namespace MysticRiver.Client.Services;

public sealed class BattleApiClient(HttpClient httpClient) {
    private readonly HttpClient _httpClient = httpClient;
    private string? playerToken;

    public void SetPlayerToken(string? token) {
        playerToken = string.IsNullOrWhiteSpace(token) ? null : token;
    }

    public async Task<StartBattleResponse> StartBattleAsync(StartBattleRequest? request = null, CancellationToken cancellationToken = default) {
        var payload = request ?? new StartBattleRequest();
        using var response = await _httpClient.PostAsJsonAsync("api/battles/start", payload, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<StartBattleResponse>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Battle start response was empty.");
    }

    public async Task<IReadOnlyList<AbilityDefinitionDto>> GetAbilitiesAsync(CancellationToken cancellationToken = default)
    {
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/battles/abilities");
        if (playerToken is not null) { request.Headers.Add("X-Player-Token", playerToken); }
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<IReadOnlyList<AbilityDefinitionDto>>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Ability catalog response was empty.");
    }

    public async Task<BattleStateDto> ExecuteAbilityAsync(
        string battleId,
        ExecuteAbilityRequest request,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

        using var msg = new HttpRequestMessage(HttpMethod.Post, $"api/battles/{battleId}/actions/ability") {
            Content = JsonContent.Create(request)
        };
        if (playerToken is not null) { msg.Headers.Add("X-Player-Token", playerToken); }
        using var response = await _httpClient.SendAsync(msg, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Battle state response was empty.");
    }

    public async Task<BattleStateDto> GetBattleStateAsync(string battleId, CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        using var msg = new HttpRequestMessage(HttpMethod.Get, $"api/battles/{battleId}");
        if (playerToken is not null) { msg.Headers.Add("X-Player-Token", playerToken); }
        using var response = await _httpClient.SendAsync(msg, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("GetBattleState response was empty.");
    }

    public async Task<BattleStateDto> AbandonBattleAsync(
        string battleId,
        AbandonBattleRequest request,
        CancellationToken cancellationToken = default) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

                using var msg = new HttpRequestMessage(HttpMethod.Post, $"api/battles/{battleId}/abandon") {
            Content = JsonContent.Create(request)
        };
        if (playerToken is not null) { msg.Headers.Add("X-Player-Token", playerToken); }
        using var response = await _httpClient.SendAsync(msg, cancellationToken);
        response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken: cancellationToken);
        return result ?? throw new InvalidOperationException("Abandon battle response was empty.");
    }
}
