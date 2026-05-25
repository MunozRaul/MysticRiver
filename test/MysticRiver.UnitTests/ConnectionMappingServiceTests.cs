using System;
using System.Reflection;
using System.Threading.Tasks;
using Xunit;
using MysticRiver.HttpApi.Battles;

namespace MysticRiver.UnitTests;

public class ConnectionMappingServiceTests {
    [Fact]
    public void CreateToken_Then_TryGetByToken_ReturnsExpectedMappings() {
        var svc = new ConnectionMappingService();
        svc.Register("conn-1", "battle-1", "player-1", "Guest");
        var token = svc.CreateToken("conn-1", "battle-1", "player-1", "Guest");

        var ok = svc.TryGetByToken(token, out var battleId, out var playerId, out var displayName);

        Assert.True(ok);
        Assert.Equal("battle-1", battleId);
        Assert.Equal("player-1", playerId);
        Assert.Equal("Guest", displayName);
    }

    [Fact]
    public void RemoveExpiredTokens_RemovesTokensOlderThanTtl() {
        var svc = new ConnectionMappingService();
        svc.Register("conn-2", "battle-2", "player-2", "Guest2");
        var token = svc.CreateToken("conn-2", "battle-2", "player-2", "Guest2");

        // Use reflection to set the internal createdAt to an old value so it appears expired
        var field = typeof(ConnectionMappingService).GetField("_tokens", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(field);
        var dict = (System.Collections.IDictionary)field.GetValue(svc)!;

        // Retrieve the tuple value and reconstruct with old CreatedAt
        var entry = dict[token];
        Assert.NotNull(entry);

        // The value is a ValueTuple; access elements by Item1..Item6 since names may not be preserved at runtime
        dynamic v = entry;
        string connectionId = v.Item1;
        string battleId = v.Item2;
        string playerId = v.Item3;
        string? displayName = v.Item4;
        DateTimeOffset createdAt = v.Item5;
        bool singleUse = v.Item6;

        // Replace with an older CreatedAt
        var old = DateTimeOffset.UtcNow - TimeSpan.FromHours(1);
        var newValue = (connectionId, battleId, playerId, displayName, old, singleUse);
        dict[token] = newValue;

        // Sweep expired tokens
        svc.RemoveExpiredTokens();

        var okAfter = svc.TryGetByToken(token, out var _, out var _, out var _);
        Assert.False(okAfter);
    }
}
