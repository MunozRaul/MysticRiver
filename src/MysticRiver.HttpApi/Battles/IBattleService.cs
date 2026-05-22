using MysticRiver.Contracts.Battle;

namespace MysticRiver.HttpApi.Battles;

public interface IBattleService {
    StartBattleResponse StartBattle(StartBattleRequest request);
    IReadOnlyList<AbilityDefinitionDto> GetAbilities();
    BattleStateDto ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request);
    BattleStateDto ExecuteAbility(string battleId, ExecuteAbilityRequest request);
}
