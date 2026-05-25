namespace MysticRiver.Contracts.Battle;

public sealed record BattleLifecycleEvent(
    string BattleId,
    BattleLifecycleEventKind Kind,
    string? PlayerId = null,
    string? DisplayName = null,
    BattleEndReason EndReason = BattleEndReason.None,
    string? WinnerCreatureId = null);
