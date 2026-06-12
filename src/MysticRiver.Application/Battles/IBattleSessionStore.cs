using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

public interface IBattleSessionStore {
    BattleSession Create(StartBattleRequest request);
    bool TryGet(string battleId, out BattleSession session);
}
