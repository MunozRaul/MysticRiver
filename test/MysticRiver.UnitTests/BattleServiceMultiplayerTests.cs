using MysticRiver.Application.Battles;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.UnitTests;

public class BattleServiceMultiplayerTests {
    [Fact]
    public void ExecuteAbility_Multiplayer_DoesNotApplyAutoCounter() {
        var service = CreateService();
        var hostPlayerId = "host-player";
        var guestPlayerId = "guest-player";

        var created = service.CreateMatch(new CreateMatchRequest(hostPlayerId, HostInitiative: 30, OpponentInitiative: 10));
        _ = service.JoinMatch(created.BattleId, new JoinMatchRequest(guestPlayerId));

        var before = service.GetBattleState(created.BattleId);
        var request = new ExecuteAbilityRequest("basic-attack", BattleParticipantIds.Player, BattleParticipantIds.Enemy);

        var result = service.ExecuteAbility(created.BattleId, request, hostPlayerId);

        Assert.Equal(before.Creature1.CurrentHp, result.State.Creature1.CurrentHp);
        Assert.True(result.State.Creature2.CurrentHp < before.Creature2.CurrentHp);
    }

    [Fact]
    public void ExecuteAbility_Multiplayer_OutOfTurnAction_ThrowsInvalidOperationException() {
        var service = CreateService();
        var hostPlayerId = "host-player";
        var guestPlayerId = "guest-player";

        var created = service.CreateMatch(new CreateMatchRequest(hostPlayerId, HostInitiative: 30, OpponentInitiative: 10));
        _ = service.JoinMatch(created.BattleId, new JoinMatchRequest(guestPlayerId));

        var first = new ExecuteAbilityRequest("basic-attack", BattleParticipantIds.Player, BattleParticipantIds.Enemy);
        _ = service.ExecuteAbility(created.BattleId, first, hostPlayerId);

        var second = new ExecuteAbilityRequest("basic-attack", BattleParticipantIds.Player, BattleParticipantIds.Enemy);
        var ex = Assert.Throws<InvalidOperationException>(() => service.ExecuteAbility(created.BattleId, second, hostPlayerId));
        Assert.Contains("turn", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExecuteAbility_Multiplayer_PlayerCannotControlOtherCreature() {
        var service = CreateService();
        var hostPlayerId = "host-player";
        var guestPlayerId = "guest-player";

        var created = service.CreateMatch(new CreateMatchRequest(hostPlayerId, HostInitiative: 30, OpponentInitiative: 10));
        _ = service.JoinMatch(created.BattleId, new JoinMatchRequest(guestPlayerId));

        var request = new ExecuteAbilityRequest("basic-attack", BattleParticipantIds.Player, BattleParticipantIds.Enemy);
        var ex = Assert.Throws<InvalidOperationException>(() => service.ExecuteAbility(created.BattleId, request, guestPlayerId));
        Assert.Contains("own", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static IBattleService CreateService() {
        var store = new InMemoryBattleSessionStore();
        return new BattleService(store);
    }
}
