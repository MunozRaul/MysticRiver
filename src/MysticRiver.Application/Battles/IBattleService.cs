using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

public interface IBattleService {
    StartBattleResponse StartBattle(StartBattleRequest request);
    BattleStateDto GetBattleState(string battleId);
    IReadOnlyList<AbilityDefinitionDto> GetAbilities();
    BattleActionResult ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request);
    BattleActionResult ExecuteAbility(string battleId, ExecuteAbilityRequest request);
}
