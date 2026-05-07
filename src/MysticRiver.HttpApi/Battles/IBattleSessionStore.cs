using MysticRiver.Contracts.Battle;

namespace MysticRiver.HttpApi.Battles;

public interface IBattleSessionStore {
    BattleSession Create(StartBattleRequest request);
    bool TryGet(string battleId, out BattleSession session);
}
