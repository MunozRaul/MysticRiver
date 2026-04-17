using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using MysticRiver.Contracts.Battle;

namespace MysticRiver.IntegrationTests;

public sealed class BattlesApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory = factory;

    [Fact]
    public async Task StartBattle_ReturnsInitialBattleState()
    {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.PostAsJsonAsync("/api/battles/start", new StartBattleRequest(), cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<StartBattleResponse>(cancellationToken);

        Assert.NotNull(payload);
        Assert.NotEmpty(payload.BattleId);
        Assert.Equal(payload.BattleId, payload.State.BattleId);
        Assert.Equal(1, payload.State.RoundNumber);
        Assert.False(payload.State.BattleEnded);
        Assert.Null(payload.State.WinnerCreatureId);
    }

    [Fact]
    public async Task ExecuteBasicAttack_ReturnsUpdatedDeterministicState()
    {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var startResponse = await client.PostAsJsonAsync("/api/battles/start", new StartBattleRequest(), cancellationToken);
        startResponse.EnsureSuccessStatusCode();
        var startedBattle = await startResponse.Content.ReadFromJsonAsync<StartBattleResponse>(cancellationToken);
        Assert.NotNull(startedBattle);

        using var attackResponse = await client.PostAsJsonAsync(
            $"/api/battles/{startedBattle.BattleId}/actions/basic-attack",
            new ExecuteBasicAttackRequest(),
            cancellationToken);
        attackResponse.EnsureSuccessStatusCode();

        var state = await attackResponse.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken);
        Assert.NotNull(state);
        Assert.Equal(2, state.RoundNumber);
        Assert.Equal(108, state.Creature1.CurrentHp);
        Assert.Equal(90, state.Creature2.CurrentHp);
        Assert.False(state.BattleEnded);
        Assert.Null(state.WinnerCreatureId);
    }

    [Fact]
    public async Task ExecuteBasicAttack_ForUnknownBattle_ReturnsNotFound()
    {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        using var response = await client.PostAsJsonAsync(
            "/api/battles/does-not-exist/actions/basic-attack",
            new ExecuteBasicAttackRequest(),
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
