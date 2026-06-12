namespace MysticRiver.Contracts.Battle;

public sealed record BattleStateUpdatedEvent(
    string BattleId,
    BattleStateDto State,
    IReadOnlyList<BattleActionSummaryDto>? ActionSummaries = null);
