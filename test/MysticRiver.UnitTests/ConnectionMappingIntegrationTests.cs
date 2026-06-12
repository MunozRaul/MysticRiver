using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using MysticRiver.HttpApi.Battles;
using MysticRiver.HttpApi.Controllers;
using MysticRiver.Application.Battles;
using MysticRiver.Application.Data;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.UnitTests;

public class ConnectionMappingTests {
    [Fact]
    public void SingleUseToken_IsConsumedOnFirstLookup() {
        var svc = new ConnectionMappingService();
        svc.Register("c1", "b1", "p1");
        var token = svc.CreateToken("c1", "b1", "p1", singleUse: true);

        var ok1 = svc.TryGetByToken(token, out var b1, out var p1, out var d1);
        var ok2 = svc.TryGetByToken(token, out var b2, out var p2, out var d2);

        Assert.True(ok1);
        Assert.False(ok2);
    }

    [Fact]
    public void Unregister_RemovesAssociatedTokens() {
        var svc = new ConnectionMappingService();
        svc.Register("c2", "b2", "p2");
        var token = svc.CreateToken("c2", "b2", "p2");

        svc.Unregister("c2");

        var ok = svc.TryGetByToken(token, out var _, out var _, out var _);
        Assert.False(ok);
    }

    [Fact]
    public async Task TokenSweeperService_RemovesExpiredTokens_WhenRunning() {
        var svc = new ConnectionMappingService();
        svc.Register("c3", "b3", "p3", "G3");
        var token = svc.CreateToken("c3", "b3", "p3", "G3");

        // Age the token via reflection
        var field = typeof(ConnectionMappingService).GetField("_tokens", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        Assert.NotNull(field);
        var dictObj = field.GetValue(svc);
        Assert.NotNull(dictObj);
        var dict = (System.Collections.IDictionary)dictObj!;
        var entry = dict[token];
        Assert.NotNull(entry);
        dynamic v = entry;
        var old = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        // Construct a ValueTuple instance of the exact value type expected by the concurrent dictionary
        var valueType = field.FieldType.GetGenericArguments()[1];
        var newValueObj = Activator.CreateInstance(valueType, new object[] { v.Item1, v.Item2, v.Item3, v.Item4, old, v.Item6 });
        dict[token] = newValueObj!;

        var logger = NullLogger<TokenSweeperService>.Instance;
        var sweeper = new TokenSweeperService(svc, logger, TimeSpan.FromMilliseconds(200));

        // Start the sweeper (use CancellationToken.None to satisfy analyzer guidance) and let it run briefly
#pragma warning disable xUnit1051
        var runTask = sweeper.StartAsync(CancellationToken.None);
        await Task.Delay(500);
        await sweeper.StopAsync(CancellationToken.None);
#pragma warning restore xUnit1051

        var ok = svc.TryGetByToken(token, out var _, out var _, out var _);
        Assert.False(ok);
    }

    [Fact]
    public async Task AbandonEndpoint_AllowsAuthorizedPlayer_ToForfeitAndMarksWinner() {
        var sessionStore = new InMemoryBattleSessionStore();
        var battleService = new BattleService(sessionStore);

        // Start a battle
        var startReq = new StartBattleRequest("Tester", "Enemy", 100, 100, 10, 10, 5);
        var startResp = battleService.StartBattle(startReq);
        var battleId = startResp.BattleId;
        var playerCreatureId = startResp.State.Creature1.CreatureId;

        var mapping = new ConnectionMappingService();
        // create token claiming the playerCreatureId
        var token = mapping.CreateToken("conn-abandon", battleId, playerCreatureId, "Tester");

        // build a fake hub context that does nothing
        var fakeClients = new FakeHubClients();
        var fakeHubContext = new FakeHubContext(fakeClients);
        var logger = NullLogger<BattlesController>.Instance;

        var dbOptions = new DbContextOptionsBuilder<MysticRiverDbContext>()
            .UseInMemoryDatabase("test-abandon")
            .Options;
        var dbContext = new MysticRiverDbContext(dbOptions);
        var persistenceSvc = new BattleSessionPersistenceService(dbContext);

        var controller = new BattlesController(battleService, fakeHubContext, mapping, persistenceSvc, logger);
        // Set up HttpContext with header
        controller.ControllerContext = new Microsoft.AspNetCore.Mvc.ControllerContext {
            HttpContext = new DefaultHttpContext()
        };
        controller.Request.Headers["X-Player-Token"] = token;

        var abandonReq = new AbandonBattleRequest(playerCreatureId);
        var result = await controller.AbandonBattle(battleId, abandonReq);

        // Expect OK and battle concluded
        var objectResult = result.Result as Microsoft.AspNetCore.Mvc.ObjectResult;
        Assert.NotNull(objectResult);
        var state = objectResult!.Value as BattleStateDto;
        Assert.NotNull(state);
        Assert.True(state!.BattleEnded);
    }

    // Minimal fake hub client/context implementations
    private sealed class FakeHubClients : IHubClients<IBattleClient> {
        public IBattleClient All => new FakeBattleClient();
        public IBattleClient AllExcept(System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new FakeBattleClient();
        public IBattleClient Client(string connectionId) => new FakeBattleClient();
        public IBattleClient Clients(System.Collections.Generic.IReadOnlyList<string> connectionIds) => new FakeBattleClient();
        public IBattleClient Group(string groupName) => new FakeBattleClient();
        public IBattleClient GroupExcept(string groupName, System.Collections.Generic.IReadOnlyList<string> excludedConnectionIds) => new FakeBattleClient();
        public IBattleClient Groups(System.Collections.Generic.IReadOnlyList<string> groupNames) => new FakeBattleClient();
        public IBattleClient User(string userId) => new FakeBattleClient();
        public IBattleClient Users(System.Collections.Generic.IReadOnlyList<string> userIds) => new FakeBattleClient();
    }

    private sealed class FakeBattleClient : IBattleClient {
        public Task BattleStateUpdated(BattleStateUpdatedEvent battleStateUpdatedEvent) => Task.CompletedTask;
        public Task BattleLifecycleUpdated(BattleLifecycleEvent battleLifecycleEvent) => Task.CompletedTask;
    }

    private sealed class FakeGroupManager : IGroupManager {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeHubContext : IHubContext<BattleHub, IBattleClient> {
        public FakeHubContext(IHubClients<IBattleClient> clients) {
            Clients = clients;
            Groups = new FakeGroupManager();
        }

        public IHubClients<IBattleClient> Clients { get; }
        public IGroupManager Groups { get; }
    }
}
