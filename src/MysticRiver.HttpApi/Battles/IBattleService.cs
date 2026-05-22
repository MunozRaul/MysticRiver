using MysticRiver.Contracts.Battle;

namespace MysticRiver.HttpApi.Battles;

public interface IBattleService {
    StartBattleResponse StartBattle(StartBattleRequest request);
    BattleStateDto GetBattleState(string battleId);
    IReadOnlyList<AbilityDefinitionDto> GetAbilities();
    BattleStateDto ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request);
    BattleStateDto ExecuteAbility(string battleId, ExecuteAbilityRequest request);
}
