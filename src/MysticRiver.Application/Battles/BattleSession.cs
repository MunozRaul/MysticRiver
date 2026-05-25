using MysticRiver.Domain;
using MysticRiver.Contracts.Battle;

namespace MysticRiver.Application.Battles;

public sealed class BattleSession {
    private readonly Dictionary<string, Creature> _creaturesById;
    private readonly object _syncRoot = new();
    private string? _forcedWinnerCreatureId;

    public string BattleId { get; }
    public Battle Battle { get; }
    public int RoundNumber { get; private set; }
    public int StateVersion { get; private set; }
    public int EnemyAttackPower { get; }
    public MatchStatus MatchStatus { get; private set; }
    public string? HostPlayerId { get; private set; }
    public string? GuestPlayerId { get; private set; }
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
}
