using MysticRiver.Domain;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

public sealed class BattleSession {
    private readonly Dictionary<string, Creature> _creaturesById;
    private readonly object _syncRoot = new();
    private string? _forcedWinnerCreatureId;
    private string? _currentTurnCreatureId;

    public string BattleId { get; }
    public Battle Battle { get; }
    public int RoundNumber { get; private set; }
    public int StateVersion { get; private set; }
    public int EnemyAttackPower { get; }
    public MatchStatus MatchStatus { get; private set; }
    public string? HostPlayerId { get; private set; }
    public string? GuestPlayerId { get; private set; }
    public string? CurrentTurnCreatureId => _currentTurnCreatureId;
    public string? ForcedWinnerCreatureId => _forcedWinnerCreatureId;
    public bool IsConcluded => _forcedWinnerCreatureId is not null || Battle.IsOver;
    public object SyncRoot => _syncRoot;

    public BattleSession(string battleId, Battle battle, int enemyAttackPower) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(battle);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(enemyAttackPower);

        BattleId = battleId;
        Battle = battle;
        EnemyAttackPower = enemyAttackPower;
        RoundNumber = 1;
        StateVersion = 1;
        MatchStatus = MatchStatus.InProgress;
        _creaturesById = new Dictionary<string, Creature>(StringComparer.OrdinalIgnoreCase) {
            [BattleParticipantIds.Player] = battle.Creature1,
            [BattleParticipantIds.Enemy] = battle.Creature2
        };
    }

    public void InitializeLobby(string hostPlayerId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(hostPlayerId);

        if (HostPlayerId is not null) {
            throw new InvalidOperationException("Host player is already assigned.");
        }

        HostPlayerId = hostPlayerId;
        MatchStatus = MatchStatus.WaitingForOpponent;
        StateVersion++;
    }

    public void JoinGuest(string guestPlayerId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(guestPlayerId);

        if (HostPlayerId is null) {
            throw new InvalidOperationException("Cannot join a match without a host.");
        }

        if (GuestPlayerId is not null) {
            throw new InvalidOperationException("This match already has a second player.");
        }

        if (string.Equals(HostPlayerId, guestPlayerId, StringComparison.OrdinalIgnoreCase)) {
            throw new ArgumentException("Host and guest players must be different.");
        }

        GuestPlayerId = guestPlayerId;
        MatchStatus = MatchStatus.Ready;
        _currentTurnCreatureId = DetermineInitialTurnCreatureId();
        StateVersion++;
    }

    public Creature GetRequiredCreature(string creatureId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(creatureId);

        if (_creaturesById.TryGetValue(creatureId, out var creature)) {
            return creature;
        }

        throw new KeyNotFoundException($"Creature '{creatureId}' does not exist in battle '{BattleId}'.");
    }

    public string GetCreatureId(Creature creature) {
        ArgumentNullException.ThrowIfNull(creature);

        foreach (var entry in _creaturesById) {
            if (ReferenceEquals(entry.Value, creature)) {
                return entry.Key;
            }
        }

        throw new KeyNotFoundException($"Creature '{creature.Name}' does not exist in battle '{BattleId}'.");
    }

    public void AdvanceRound() {
        if (MatchStatus == MatchStatus.Ready) {
            MatchStatus = MatchStatus.InProgress;
        }

        if (IsMultiplayerMatch() && _currentTurnCreatureId is not null && !Battle.IsOver) {
            _currentTurnCreatureId = string.Equals(_currentTurnCreatureId, BattleParticipantIds.Player, StringComparison.OrdinalIgnoreCase)
                ? BattleParticipantIds.Enemy
                : BattleParticipantIds.Player;
        }

        RoundNumber++;
        StateVersion++;

        if (Battle.IsOver) {
            MatchStatus = MatchStatus.Completed;
        }
    }

    public void Concede(string abandoningCreatureId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(abandoningCreatureId);

        if (IsConcluded) {
            throw new InvalidOperationException("Battle is already over.");
        }

        var abandoningCreature = GetRequiredCreature(abandoningCreatureId);
        var winnerCreature = ReferenceEquals(abandoningCreature, Battle.Creature1) ? Battle.Creature2 : Battle.Creature1;
        _forcedWinnerCreatureId = GetCreatureId(winnerCreature);
        MatchStatus = MatchStatus.Completed;
        StateVersion++;
    }

    public void EnsureActionsAllowed() {
        if (MatchStatus == MatchStatus.WaitingForOpponent) {
            throw new InvalidOperationException("Match is waiting for an opponent.");
        }
    }

    public bool IsMultiplayerMatch() => HostPlayerId is not null && GuestPlayerId is not null;

    public string GetAssignedCreatureIdForPlayer(string playerId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);

        if (!IsMultiplayerMatch()) {
            throw new InvalidOperationException("Player-to-creature assignment is only available for multiplayer matches.");
        }

        if (string.Equals(playerId, HostPlayerId, StringComparison.OrdinalIgnoreCase)) {
            return BattleParticipantIds.Player;
        }

        if (string.Equals(playerId, GuestPlayerId, StringComparison.OrdinalIgnoreCase)) {
            return BattleParticipantIds.Enemy;
        }

        throw new InvalidOperationException("Player does not belong to this match.");
    }

    public void EnsurePlayerOwnsCreature(string playerId, string creatureId) {
        var expectedCreatureId = GetAssignedCreatureIdForPlayer(playerId);
        if (!string.Equals(expectedCreatureId, creatureId, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("Player does not own the requested creature.");
        }
    }

    public void EnsureCurrentTurnCreature(string creatureId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(creatureId);

        if (!IsMultiplayerMatch()) {
            return;
        }

        if (_currentTurnCreatureId is null) {
            throw new InvalidOperationException("Turn order is not initialized for this match.");
        }

        if (!string.Equals(_currentTurnCreatureId, creatureId, StringComparison.OrdinalIgnoreCase)) {
            throw new InvalidOperationException("It is not this creature's turn.");
        }
    }

    public string GetOpponentCreatureId(string creatureId) {
        return string.Equals(creatureId, BattleParticipantIds.Player, StringComparison.OrdinalIgnoreCase)
            ? BattleParticipantIds.Enemy
            : BattleParticipantIds.Player;
    }

    private string DetermineInitialTurnCreatureId() {
        var creature1 = Battle.Creature1;
        var creature2 = Battle.Creature2;

        if (creature1.EffectiveInitiative > creature2.EffectiveInitiative) {
            return BattleParticipantIds.Player;
        }

        if (creature2.EffectiveInitiative > creature1.EffectiveInitiative) {
            return BattleParticipantIds.Enemy;
        }

        return BattleParticipantIds.Player;
    }
}
