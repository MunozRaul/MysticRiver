using MysticRiver.Contracts.Battle;
using MysticRiver.Domain;
using ContractAbilityTag = MysticRiver.Contracts.Battle.AbilityTag;
using ContractAbilityTarget = MysticRiver.Contracts.Battle.AbilityTarget;
using ContractCrowdControlKind = MysticRiver.Contracts.Battle.CrowdControlKind;
using ContractStatusEffect = MysticRiver.Contracts.Battle.StatusEffect;
using DomainAbilityTag = MysticRiver.Domain.AbilityTag;
using DomainAbilityTarget = MysticRiver.Domain.AbilityTarget;
using DomainCrowdControlKind = MysticRiver.Domain.CrowdControlKind;
using DomainStatusEffect = MysticRiver.Domain.StatusEffect;

namespace MysticRiver.Application.Battles;

public sealed class BattleService(IBattleSessionStore battleSessionStore) : IBattleService {
    private readonly IBattleSessionStore _battleSessionStore = battleSessionStore;

    public CreateMatchResponse CreateMatch(CreateMatchRequest request) {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.HostPlayerId);

        var startBattleRequest = new StartBattleRequest(
            request.HostDisplayName,
            request.OpponentDisplayName,
            request.HostMaxHp,
            request.OpponentMaxHp,
            request.HostInitiative,
            request.OpponentInitiative,
            request.OpponentAttackPower);

        var session = _battleSessionStore.Create(startBattleRequest);
        lock (session.SyncRoot) {
            session.InitializeLobby(request.HostPlayerId);
            var state = MapState(session);
            return new CreateMatchResponse(
                session.BattleId,
                session.MatchStatus,
                request.HostPlayerId,
                BattleParticipantIds.Player,
                state);
        }
    }

    public JoinMatchResponse JoinMatch(string battleId, JoinMatchRequest request) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.GuestPlayerId);

        var session = GetRequiredSession(battleId);

        lock (session.SyncRoot) {
            session.JoinGuest(request.GuestPlayerId);
            if (!string.IsNullOrWhiteSpace(request.GuestDisplayName)) {
                session.Battle.Creature2.Name = request.GuestDisplayName;
            }

            var hostPlayerId = session.HostPlayerId
                ?? throw new InvalidOperationException("Host player assignment is missing for this match.");
            var guestPlayerId = session.GuestPlayerId
                ?? throw new InvalidOperationException("Guest player assignment is missing for this match.");
            var state = MapState(session);

            return new JoinMatchResponse(
                session.BattleId,
                session.MatchStatus,
                hostPlayerId,
                guestPlayerId,
                BattleParticipantIds.Player,
                BattleParticipantIds.Enemy,
                state);
        }
    }

    public StartBattleResponse StartBattle(StartBattleRequest request) {
        ArgumentNullException.ThrowIfNull(request);
        var session = _battleSessionStore.Create(request);

        return new StartBattleResponse(
            session.BattleId,
            MapState(session));
    }

    public BattleStateDto GetBattleState(string battleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);

        if (!_battleSessionStore.TryGet(battleId, out var session))
        {
            throw new KeyNotFoundException($"Battle '{battleId}' was not found.");
        }

        lock (session.SyncRoot)
        {
            return MapState(session);
        }
    }

    public BattleSession GetSession(string battleId) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        return GetRequiredSession(battleId);
    }

    public bool RequiresPlayerToken(string battleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        var session = GetRequiredSession(battleId);

        lock (session.SyncRoot)
        {
            return session.HostPlayerId is not null || session.GuestPlayerId is not null;
        }
    }

    public void ValidateRealtimeJoin(string battleId, string playerId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentException.ThrowIfNullOrWhiteSpace(playerId);
        var session = GetRequiredSession(battleId);

        lock (session.SyncRoot)
        {
            if (session.IsMultiplayerMatch()) {
                _ = session.GetAssignedCreatureIdForPlayer(playerId);
                return;
            }

            if (session.HostPlayerId is not null && string.Equals(session.HostPlayerId, playerId, StringComparison.OrdinalIgnoreCase)) {
                return;
            }

            _ = session.GetRequiredCreature(playerId);
        }
    }

    public BattleActionResult AbandonBattle(string battleId, AbandonBattleRequest request, string? actingPlayerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

        var session = GetRequiredSession(battleId);

        lock (session.SyncRoot)
        {
            session.EnsureActionsAllowed();
            EnsurePlayerAuthorization(session, actingPlayerId, request.AbandoningCreatureId);
            var abandoningCreature = session.GetRequiredCreature(request.AbandoningCreatureId);
            session.Concede(request.AbandoningCreatureId);

            var winnerId = session.ForcedWinnerCreatureId
                ?? throw new InvalidOperationException("Battle forfeit did not resolve a winner.");

            var winnerCreature = session.GetRequiredCreature(winnerId);
            var forfeitAbility = new AbilityDefinitionDto("forfeit", "Forfeit", ContractAbilityTarget.Self, default, 0);
            var summary = new BattleActionSummaryDto(
                forfeitAbility,
                session.GetCreatureId(abandoningCreature),
                null,
                Array.Empty<AppliedEffectDto>());

            var state = MapState(session);
            _ = winnerCreature;
            return new BattleActionResult(state, new[] { summary });
        }
    }

    public IReadOnlyList<AbilityDefinitionDto> GetAbilities()
    {
        return AbilityCatalog.All
            .Select(MapAbility)
            .ToList();
    }

    public BattleActionResult ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request, string? actingPlayerId = null) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

        var session = GetRequiredSession(battleId);

        lock (session.SyncRoot) {
            session.EnsureActionsAllowed();
            EnsurePlayerAuthorization(session, actingPlayerId, request.AttackerId);
            session.EnsureCurrentTurnCreature(request.AttackerId);
            var attacker = session.GetRequiredCreature(request.AttackerId);
            var target = session.GetRequiredCreature(request.TargetId);

            ValidateDifferentCreatures(attacker, target);

            var attackMove = new DamageMove(request.Power, DamageKind.Physical) {
                Source = attacker,
                Destination = target
            };
            var abilityDefinition = AbilityCatalog.TryGetById("basic-attack", out var ability)
                ? MapAbility(ability!)
                : new AbilityDefinitionDto("basic-attack", "Basic Attack", ContractAbilityTarget.Enemy, ContractAbilityTag.Damage, 0);
            var summary = CreateActionSummary(session, abilityDefinition, attackMove);

            if (session.IsMultiplayerMatch()) {
                var state = ExecuteTurnWithoutAutoCounter(session, attackMove, request.AttackerId);
                return new BattleActionResult(state, new[] { summary });
            }

            // Single-player fallback keeps deterministic counter behavior.
            var (counterAttacker, counterTarget) = GetCounterPair(session, attacker);
            var counterMove = CreateCounterMove(session, counterAttacker, counterTarget);
            var counterAbility = new AbilityDefinitionDto("counter-attack", "Counter Attack", ContractAbilityTarget.Enemy, ContractAbilityTag.Damage, 0);
            var counterSummary = CreateActionSummary(session, counterAbility, counterMove);
            var counterState = ExecuteTurnWithCounter(session, attackMove, counterMove);
            return new BattleActionResult(counterState, new[] { summary, counterSummary });
        }
    }

    public BattleActionResult ExecuteAbility(string battleId, ExecuteAbilityRequest request, string? actingPlayerId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

        var session = GetRequiredSession(battleId);

        if (!AbilityCatalog.TryGetById(request.AbilityId, out var ability) || ability is null)
        {
            throw new ArgumentException($"Ability '{request.AbilityId}' does not exist.");
        }

        lock (session.SyncRoot)
        {
            session.EnsureActionsAllowed();
            EnsurePlayerAuthorization(session, actingPlayerId, request.AttackerId);
            session.EnsureCurrentTurnCreature(request.AttackerId);
            var attacker = session.GetRequiredCreature(request.AttackerId);
            var target = ResolveAndValidateTarget(session, attacker, ability, request.TargetId);

            var move = ability.CreateMove(attacker, target);
            var summary = CreateActionSummary(session, MapAbility(ability), move);

            if (session.IsMultiplayerMatch()) {
                var state = ExecuteTurnWithoutAutoCounter(session, move, request.AttackerId);
                return new BattleActionResult(state, new[] { summary });
            }

            var (counterAttacker, counterTarget) = GetCounterPair(session, attacker);
            var counterMove = CreateCounterMove(session, counterAttacker, counterTarget);
            var counterAbility = new AbilityDefinitionDto("counter-attack", "Counter Attack", ContractAbilityTarget.Enemy, ContractAbilityTag.Damage, 0);
            var counterSummary = CreateActionSummary(session, counterAbility, counterMove);
            var counterState = ExecuteTurnWithCounter(session, move, counterMove);
            return new BattleActionResult(counterState, new[] { summary, counterSummary });
        }
    }

    private BattleSession GetRequiredSession(string battleId)
    {
        if (!_battleSessionStore.TryGet(battleId, out var session))
        {
            throw new KeyNotFoundException($"Battle '{battleId}' was not found.");
        }

        return session;
    }

    private Creature ResolveAndValidateTarget(BattleSession session, Creature attacker, AbilityDefinition ability, string? targetId)
    {
        Creature? target = null;

        if (!string.IsNullOrWhiteSpace(targetId))
        {
            target = session.GetRequiredCreature(targetId);
        }

        if (ability.Target == DomainAbilityTarget.Enemy)
        {
            if (target is null)
            {
                throw new ArgumentException("TargetId is required for enemy abilities.");
            }

            ValidateDifferentCreatures(attacker, target);
        }
        else
        {
            if (target is not null && !ReferenceEquals(attacker, target))
            {
                throw new ArgumentException("Self-targeted abilities must target the attacker.");
            }

            target = attacker;
        }

        return target;
    }

    private static void ValidateDifferentCreatures(Creature creature1, Creature creature2)
    {
        if (ReferenceEquals(creature1, creature2))
        {
            throw new ArgumentException("Attacker and target must be different creatures.");
        }
    }

    private static BattleStateDto MapState(BattleSession session) {
        var creature1Id = session.GetCreatureId(session.Battle.Creature1);
        var creature2Id = session.GetCreatureId(session.Battle.Creature2);
        var creature1 = MapCreature(session.Battle.Creature1, creature1Id);
        var creature2 = MapCreature(session.Battle.Creature2, creature2Id);
        var endReason = BattleEndReason.None;
        var winnerId = session.ForcedWinnerCreatureId
            ?? (session.Battle.TryGetResult(out var result)
                ? session.GetCreatureId(result!.Winner)
                : null);

        if (session.ForcedWinnerCreatureId is not null) {
            endReason = session.ForcedEndReason;
        }
        else if (session.Battle.TryGetResult(out var _)) {
            endReason = BattleEndReason.Eliminated;
        }

        return new BattleStateDto(
            session.BattleId,
            session.RoundNumber,
            session.StateVersion,
            creature1,
            creature2,
            session.CurrentTurnCreatureId,
            session.IsConcluded,
            winnerId,
            session.MatchStatus,
            endReason);
    }

    private static BattleCreatureDto MapCreature(Creature creature, string creatureId) {
        return new BattleCreatureDto(
            creatureId,
            creature.Name,
            creature.MaxHp,
            creature.CurrentHp,
            creature.MaxMana,
            creature.CurrentMana,
            creature.Initiative,
            creature.EffectiveInitiative,
            creature.CurrentShield,
            GetStatusEffects(creature),
            MapCrowdControl(creature.CrowdControl),
            creature.CrowdControlTurnsRemaining,
            creature.IsDead);
    }

    private static AbilityDefinitionDto MapAbility(AbilityDefinition definition)
    {
        return new AbilityDefinitionDto(
            definition.Id,
            definition.Name,
            MapAbilityTarget(definition.Target),
            MapAbilityTag(definition.Tags),
            definition.ManaCost);
    }

    private static ContractAbilityTarget MapAbilityTarget(DomainAbilityTarget target)
    {
        return target switch
        {
            DomainAbilityTarget.Self => ContractAbilityTarget.Self,
            DomainAbilityTarget.Enemy => ContractAbilityTarget.Enemy,
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Unknown ability target."),
        };
    }

    private static ContractAbilityTag MapAbilityTag(DomainAbilityTag tags)
    {
        return (ContractAbilityTag)tags;
    }

    private static IReadOnlyList<StatusEffectStateDto> GetStatusEffects(Creature creature)
    {
        var effects = new List<StatusEffectStateDto>();
        foreach (var effect in Enum.GetValues<DomainStatusEffect>())
        {
            if (effect == DomainStatusEffect.None || !creature.HasStatus(effect))
            {
                continue;
            }

            effects.Add(new StatusEffectStateDto(
                MapStatusEffect(effect),
                creature.GetStatusStacks(effect),
                creature.GetStatusTurnsRemaining(effect)));
        }

        return effects;
    }

    private static ContractStatusEffect MapStatusEffect(DomainStatusEffect effect)
    {
        return effect switch
        {
            DomainStatusEffect.Poison => ContractStatusEffect.Poison,
            DomainStatusEffect.Burn => ContractStatusEffect.Burn,
            DomainStatusEffect.Paralysis => ContractStatusEffect.Paralysis,
            DomainStatusEffect.Sleep => ContractStatusEffect.Sleep,
            DomainStatusEffect.Freeze => ContractStatusEffect.Freeze,
            DomainStatusEffect.Toxic => ContractStatusEffect.Toxic,
            DomainStatusEffect.Bleed => ContractStatusEffect.Bleed,
            DomainStatusEffect.Haste => ContractStatusEffect.Haste,
            DomainStatusEffect.Slow => ContractStatusEffect.Slow,
            DomainStatusEffect.None => ContractStatusEffect.None,
            _ => throw new ArgumentOutOfRangeException(nameof(effect), effect, "Unknown status effect."),
        };
    }

    private static ContractCrowdControlKind MapCrowdControl(DomainCrowdControlKind crowdControl)
    {
        if (crowdControl == DomainCrowdControlKind.None)
        {
            return ContractCrowdControlKind.None;
        }

        var mapped = ContractCrowdControlKind.None;
        if (crowdControl.HasFlag(DomainCrowdControlKind.Silence))
        {
            mapped |= ContractCrowdControlKind.Silence;
        }

        if (crowdControl.HasFlag(DomainCrowdControlKind.Stun))
        {
            mapped |= ContractCrowdControlKind.Stun;
        }

        return mapped;
    }

    private static BattleActionSummaryDto CreateActionSummary(
        BattleSession session,
        AbilityDefinitionDto ability,
        Move move)
    {
        var actorId = GetMoveActorId(session, move);
        var targetId = GetMoveTargetId(session, move);
        var appliedEffects = BuildAppliedEffects(session, move);

        return new BattleActionSummaryDto(ability, actorId, targetId, appliedEffects);
    }

    private static string GetMoveActorId(BattleSession session, Move move)
    {
        return move switch
        {
            TargetedMove targeted => session.GetCreatureId(targeted.Source),
            SelfMove self => session.GetCreatureId(self.Self),
            _ => throw new ArgumentException($"Unknown move type: {move.GetType().Name}")
        };
    }

    private static string? GetMoveTargetId(BattleSession session, Move move)
    {
        return move switch
        {
            TargetedMove targeted => session.GetCreatureId(targeted.Destination),
            SelfMove self => session.GetCreatureId(self.Self),
            _ => null
        };
    }

    private static IReadOnlyList<AppliedEffectDto> BuildAppliedEffects(BattleSession session, Move move)
    {
        var effects = new List<AppliedEffectDto>();

        switch (move)
        {
            case DamageMove damageMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Damage,
                    session.GetCreatureId(damageMove.Destination),
                    damageMove.DamageAmount));
                break;
            case StatusDamageMove statusDamageMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Damage,
                    session.GetCreatureId(statusDamageMove.Destination),
                    statusDamageMove.DamageAmount));
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Status,
                    session.GetCreatureId(statusDamageMove.Destination),
                    StatusEffect: MapStatusEffect(statusDamageMove.Effect)));
                break;
            case LifestealMove lifestealMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Damage,
                    session.GetCreatureId(lifestealMove.Destination),
                    lifestealMove.DamageAmount));
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Lifesteal,
                    session.GetCreatureId(lifestealMove.Source),
                    Ratio: lifestealMove.HealRatio));
                break;
            case HealMove healMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Heal,
                    session.GetCreatureId(healMove.Self),
                    healMove.HealAmount));
                break;
            case ShieldMove shieldMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Shield,
                    session.GetCreatureId(shieldMove.Self),
                    shieldMove.ShieldAmount));
                break;
            case ManaRestoreMove manaRestoreMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.ManaRestore,
                    session.GetCreatureId(manaRestoreMove.Self),
                    manaRestoreMove.ManaAmount));
                break;
            case ManaBurnMove manaBurnMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.ManaBurn,
                    session.GetCreatureId(manaBurnMove.Self),
                    manaBurnMove.ManaAmount));
                break;
            case ManaDrainMove manaDrainMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.ManaDrain,
                    session.GetCreatureId(manaDrainMove.Destination),
                    manaDrainMove.ManaAmount));
                break;
            case StatusEffectMove statusMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Status,
                    session.GetCreatureId(statusMove.Destination),
                    StatusEffect: MapStatusEffect(statusMove.Effect)));
                break;
            case SelfStatusMove selfStatusMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.Status,
                    session.GetCreatureId(selfStatusMove.Self),
                    StatusEffect: MapStatusEffect(selfStatusMove.Effect)));
                break;
            case CrowdControlMove crowdControlMove:
                effects.Add(new AppliedEffectDto(
                    AppliedEffectKind.CrowdControl,
                    session.GetCreatureId(crowdControlMove.Destination),
                    CrowdControl: MapCrowdControl(crowdControlMove.CrowdControlType),
                    Turns: crowdControlMove.Turns));
                break;
        }

        return effects;
    }

    private static (Creature attacker, Creature target) GetCounterPair(BattleSession session, Creature attacker)
    {
        var counterAttacker = ReferenceEquals(attacker, session.Battle.Creature1)
            ? session.Battle.Creature2
            : session.Battle.Creature1;

        return (counterAttacker, attacker);
    }

    private static DamageMove CreateCounterMove(BattleSession session, Creature counterAttacker, Creature counterTarget)
    {
        return new DamageMove(session.EnemyAttackPower, DamageKind.Physical)
        {
            Source = counterAttacker,
            Destination = counterTarget,
        };
    }

    private BattleStateDto ExecuteTurnWithCounter(BattleSession session, Move move, Move counterMove)
    {
        _ = session.Battle.ExecuteTurn(move, counterMove);
        session.AdvanceRound();

        return MapState(session);
    }

    private BattleStateDto ExecuteTurnWithoutAutoCounter(BattleSession session, Move move, string actingCreatureId)
    {
        var opponentCreatureId = session.GetOpponentCreatureId(actingCreatureId);
        var opponent = session.GetRequiredCreature(opponentCreatureId);
        var actor = session.GetRequiredCreature(actingCreatureId);
        var waitMove = new DamageMove(0, DamageKind.Physical) {
            Source = opponent,
            Destination = actor
        };

        _ = session.Battle.ExecuteTurn(move, waitMove);
        session.AdvanceRound();

        return MapState(session);
    }

    private static void EnsurePlayerAuthorization(BattleSession session, string? actingPlayerId, string actingCreatureId)
    {
        if (!session.IsMultiplayerMatch()) {
            return;
        }

        if (string.IsNullOrWhiteSpace(actingPlayerId)) {
            throw new InvalidOperationException("Missing acting player identity for multiplayer action.");
        }

        session.EnsurePlayerOwnsCreature(actingPlayerId, actingCreatureId);
    }
}
