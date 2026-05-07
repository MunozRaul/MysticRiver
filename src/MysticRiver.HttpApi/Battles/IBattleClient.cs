using MysticRiver.Contracts.Battle;

namespace MysticRiver.HttpApi.Battles;

public interface IBattleClient {
    Task BattleStateUpdated(BattleStateUpdatedEvent battleStateUpdatedEvent);
}
