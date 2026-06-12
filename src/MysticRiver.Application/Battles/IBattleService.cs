using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

public interface IBattleService {
    CreateMatchResponse CreateMatch(CreateMatchRequest request);
    JoinMatchResponse JoinMatch(string battleId, JoinMatchRequest request);
    StartBattleResponse StartBattle(StartBattleRequest request);
    BattleStateDto GetBattleState(string battleId);
    BattleSession GetSession(string battleId);
    bool RequiresPlayerToken(string battleId);
    void ValidateRealtimeJoin(string battleId, string playerId);
    IReadOnlyList<AbilityDefinitionDto> GetAbilities();
    BattleActionResult ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request, string? actingPlayerId = null);
    BattleActionResult ExecuteAbility(string battleId, ExecuteAbilityRequest request, string? actingPlayerId = null);
    BattleActionResult AbandonBattle(string battleId, AbandonBattleRequest request, string? actingPlayerId = null);
}
