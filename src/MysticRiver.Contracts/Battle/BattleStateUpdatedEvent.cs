namespace MysticRiver.Contracts.Battle;

public sealed record BattleStateUpdatedEvent(
    string BattleId,
    BattleStateDto State,
    BattleActionSummaryDto? ActionSummary = null);
