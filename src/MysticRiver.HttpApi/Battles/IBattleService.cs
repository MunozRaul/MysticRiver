using MysticRiver.Contracts.Battle;

namespace MysticRiver.HttpApi.Battles;

public interface IBattleService {
    StartBattleResponse StartBattle(StartBattleRequest request);
    BattleStateDto ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request);
}
