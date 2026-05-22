using MysticRiver.Contracts.Battle;

namespace MysticRiver.HttpApi.Battles;

public sealed record BattleActionResult(
    BattleStateDto State,
    BattleActionSummaryDto ActionSummary);
