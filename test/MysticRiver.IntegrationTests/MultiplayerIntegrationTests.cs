using System.Net;
using System.Net.Http.Json;

using Microsoft.AspNetCore.Mvc.Testing;

using MysticRiver.Contracts.Battle;

namespace MysticRiver.IntegrationTests;

public sealed class MultiplayerIntegrationTests(InMemoryApiFactory factory) : IClassFixture<InMemoryApiFactory> {
    private readonly InMemoryApiFactory _factory = factory;

    [Fact]
    public async Task CreateMatch_ReturnsMatchWithWaitingForOpponentState() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var request = new CreateMatchRequest(HostPlayerId: "host-player");
        using var response = await client.PostAsJsonAsync("/api/battles/matches/create", request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);

        Assert.NotNull(payload);
        Assert.NotEmpty(payload.BattleId);
        Assert.NotEmpty(payload.HostPlayerId);
        Assert.NotNull(payload.State);
        Assert.Equal(MatchStatus.WaitingForOpponent, payload.State.MatchStatus);
        Assert.False(payload.State.BattleEnded);
    }

    [Fact]
    public async Task JoinMatch_WithValidBattleId_ReturnsReadyState() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create match
        var createRequest = new CreateMatchRequest(HostPlayerId: "host-player");
        var createResponse = await client.PostAsJsonAsync("/api/battles/matches/create", createRequest, cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createdMatch = await createResponse.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);
        Assert.NotNull(createdMatch);
        var battleId = createdMatch.BattleId;

        // Join match
        var joinRequest = new JoinMatchRequest(GuestPlayerId: "guest-player", GuestDisplayName: "GuestPlayer");
        using var joinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            joinRequest,
            cancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        var joinPayload = await joinResponse.Content.ReadFromJsonAsync<JoinMatchResponse>(cancellationToken);

        Assert.NotNull(joinPayload);
        Assert.Equal(battleId, joinPayload.BattleId);
        Assert.Equal("guest-player", joinPayload.GuestPlayerId);
        Assert.NotEmpty(joinPayload.GuestCreatureId);
        Assert.Equal(MatchStatus.Ready, joinPayload.MatchStatus);
        Assert.Equal("GuestPlayer", joinPayload.State.Creature2.Name);
    }

    [Fact]
    public async Task JoinMatch_WithNonexistentBattle_ReturnsNotFound() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        var joinRequest = new JoinMatchRequest(GuestPlayerId: "guest-player", GuestDisplayName: "GuestPlayer");
        using var response = await client.PostAsJsonAsync(
            "/api/battles/nonexistent-battle-id/matches/join",
            joinRequest,
            cancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task JoinMatch_WhenAlreadyHasGuest_ReturnsBadRequest() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create match
        var createRequest = new CreateMatchRequest(HostPlayerId: "host-player");
        var createResponse = await client.PostAsJsonAsync("/api/battles/matches/create", createRequest, cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createdMatch = await createResponse.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);
        Assert.NotNull(createdMatch);
        var battleId = createdMatch.BattleId;

        // Join first time
        var joinRequest = new JoinMatchRequest(GuestPlayerId: "guest-player-1", GuestDisplayName: "GuestPlayer1");
        var firstJoinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            joinRequest,
            cancellationToken);
        firstJoinResponse.EnsureSuccessStatusCode();

        // Try to join second time with different player
        var secondJoinRequest = new JoinMatchRequest(GuestPlayerId: "guest-player-2", GuestDisplayName: "GuestPlayer2");
        using var secondJoinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            secondJoinRequest,
            cancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, secondJoinResponse.StatusCode);
    }

    [Fact]
    public async Task TurnOwnership_HostCanActOnFirstTurn_BasedOnInitiative() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create and join match
        var createResponse = await client.PostAsJsonAsync("/api/battles/matches/create", new CreateMatchRequest(HostPlayerId: "host-player"), cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createdMatch = await createResponse.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);
        Assert.NotNull(createdMatch);
        var battleId = createdMatch.BattleId;

        var joinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            new JoinMatchRequest(GuestPlayerId: "guest-player", GuestDisplayName: "GuestPlayer"),
            cancellationToken);
        joinResponse.EnsureSuccessStatusCode();
        var joinPayload = await joinResponse.Content.ReadFromJsonAsync<JoinMatchResponse>(cancellationToken);
        Assert.NotNull(joinPayload);

        // Get battle state to check current turn
        var getStateResponse = await client.GetAsync($"/api/battles/{battleId}", cancellationToken);
        getStateResponse.EnsureSuccessStatusCode();
        var state = await getStateResponse.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken);
        Assert.NotNull(state);
        Assert.NotNull(state.CurrentTurnCreatureId);

        // Verify turn assignment is consistent with initiative
        var creature1Higher = state.Creature1.EffectiveInitiative > state.Creature2.EffectiveInitiative;
        var expectedCurrentTurn = creature1Higher ? state.Creature1.CreatureId : state.Creature2.CreatureId;
        Assert.Equal(expectedCurrentTurn, state.CurrentTurnCreatureId);
    }

    [Fact]
    public async Task ExecuteAbility_WhenNotYourTurn_ReturnsConflict() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create and join match
        var createResponse = await client.PostAsJsonAsync("/api/battles/matches/create", new CreateMatchRequest(HostPlayerId: "host-player"), cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createdMatch = await createResponse.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);
        Assert.NotNull(createdMatch);
        var battleId = createdMatch.BattleId;

        var joinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            new JoinMatchRequest(GuestPlayerId: "guest-player", GuestDisplayName: "GuestPlayer"),
            cancellationToken);
        joinResponse.EnsureSuccessStatusCode();
        var joinPayload = await joinResponse.Content.ReadFromJsonAsync<JoinMatchResponse>(cancellationToken);
        Assert.NotNull(joinPayload);

        // Get state to determine whose turn it is
        var getStateResponse = await client.GetAsync($"/api/battles/{battleId}", cancellationToken);
        getStateResponse.EnsureSuccessStatusCode();
        var state = await getStateResponse.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken);
        Assert.NotNull(state);

        // Identify which creature is NOT taking the turn
        var notCurrentTurnCreature = state.Creature1.CreatureId != state.CurrentTurnCreatureId
            ? state.Creature1.CreatureId
            : state.Creature2.CreatureId;

        // Try to execute ability as the creature that is not taking the turn
        var abilityRequest = new ExecuteAbilityRequest(
            AbilityId: "basic-attack",
            AttackerId: notCurrentTurnCreature,
            TargetId: state.Creature1.CreatureId == notCurrentTurnCreature ? state.Creature2.CreatureId : state.Creature1.CreatureId);

        using var actionResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/actions/ability",
            abilityRequest,
            cancellationToken);

        // Should fail because of turn ownership (without token validation)
        Assert.False(actionResponse.IsSuccessStatusCode);
    }

    [Fact]
    public async Task BattleState_IncludesCurrentTurnCreatureId() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create and join match
        var createResponse = await client.PostAsJsonAsync("/api/battles/matches/create", new CreateMatchRequest(HostPlayerId: "host-player"), cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createdMatch = await createResponse.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);
        Assert.NotNull(createdMatch);
        var battleId = createdMatch.BattleId;

        var joinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            new JoinMatchRequest(GuestPlayerId: "guest-player", GuestDisplayName: "GuestPlayer"),
            cancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Get battle state multiple times to verify turn tracking consistency
        for (var i = 0; i < 3; i++) {
            var getStateResponse = await client.GetAsync($"/api/battles/{battleId}", cancellationToken);
            getStateResponse.EnsureSuccessStatusCode();
            var state = await getStateResponse.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken);

            Assert.NotNull(state);
            Assert.NotNull(state.CurrentTurnCreatureId);
            Assert.True(
                state.CurrentTurnCreatureId == state.Creature1.CreatureId || state.CurrentTurnCreatureId == state.Creature2.CreatureId,
                "CurrentTurnCreatureId should match one of the creatures");
        }
    }

    [Fact]
    public async Task MatchRoomStates_TransitionCorrectly() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;

        // Create match -> WaitingForOpponent
        var createResponse = await client.PostAsJsonAsync("/api/battles/matches/create", new CreateMatchRequest(HostPlayerId: "host-player"), cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createdMatch = await createResponse.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);
        Assert.NotNull(createdMatch);
        Assert.Equal(MatchStatus.WaitingForOpponent, createdMatch.State.MatchStatus);

        var battleId = createdMatch.BattleId;

        // Join match -> Ready
        var joinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            new JoinMatchRequest(GuestPlayerId: "guest-player", GuestDisplayName: "GuestPlayer"),
            cancellationToken);
        joinResponse.EnsureSuccessStatusCode();
        var joinPayload = await joinResponse.Content.ReadFromJsonAsync<JoinMatchResponse>(cancellationToken);
        Assert.NotNull(joinPayload);
        Assert.Equal(MatchStatus.Ready, joinPayload.MatchStatus);

        // Verify state persists
        var getStateResponse = await client.GetAsync($"/api/battles/{battleId}", cancellationToken);
        getStateResponse.EnsureSuccessStatusCode();
        var state = await getStateResponse.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken);
        Assert.NotNull(state);
        Assert.Equal(MatchStatus.Ready, state.MatchStatus);
    }

    [Fact]
    public async Task GuestDisplayName_IsReflectedInBattleState() {
        using var client = _factory.CreateClient();
        var cancellationToken = TestContext.Current.CancellationToken;
        const string guestDisplayName = "CustomGuestName";

        // Create and join match with specific display name
        var createResponse = await client.PostAsJsonAsync("/api/battles/matches/create", new CreateMatchRequest(HostPlayerId: "host-player"), cancellationToken);
        createResponse.EnsureSuccessStatusCode();
        var createdMatch = await createResponse.Content.ReadFromJsonAsync<CreateMatchResponse>(cancellationToken);
        Assert.NotNull(createdMatch);
        var battleId = createdMatch.BattleId;

        var joinResponse = await client.PostAsJsonAsync(
            $"/api/battles/{battleId}/matches/join",
            new JoinMatchRequest(GuestPlayerId: "guest-player", GuestDisplayName: guestDisplayName),
            cancellationToken);
        joinResponse.EnsureSuccessStatusCode();

        // Verify guest display name in state
        var getStateResponse = await client.GetAsync($"/api/battles/{battleId}", cancellationToken);
        getStateResponse.EnsureSuccessStatusCode();
        var state = await getStateResponse.Content.ReadFromJsonAsync<BattleStateDto>(cancellationToken);
        Assert.NotNull(state);
        Assert.Equal(guestDisplayName, state.Creature2.Name);
    }
}
