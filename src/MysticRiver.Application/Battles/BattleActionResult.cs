using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

public sealed record BattleActionResult(
    BattleStateDto State,
    BattleActionSummaryDto ActionSummary);
