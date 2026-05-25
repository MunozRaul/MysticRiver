using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

public interface IBattleService {
    CreateMatchResponse CreateMatch(CreateMatchRequest request);
    JoinMatchResponse JoinMatch(string battleId, JoinMatchRequest request);
    StartBattleResponse StartBattle(StartBattleRequest request);
    BattleStateDto GetBattleState(string battleId);
    bool RequiresPlayerToken(string battleId);
    IReadOnlyList<AbilityDefinitionDto> GetAbilities();
    BattleActionResult ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request);
    BattleActionResult ExecuteAbility(string battleId, ExecuteAbilityRequest request);
    BattleActionResult AbandonBattle(string battleId, AbandonBattleRequest request);
}
