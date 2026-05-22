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

namespace MysticRiver.HttpApi.Battles;

public sealed class BattleService(IBattleSessionStore battleSessionStore) : IBattleService {
    private readonly IBattleSessionStore _battleSessionStore = battleSessionStore;

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

    public IReadOnlyList<AbilityDefinitionDto> GetAbilities()
    {
        return AbilityCatalog.All
            .Select(MapAbility)
            .ToList();
    }

    public BattleStateDto ExecuteBasicAttack(string battleId, ExecuteBasicAttackRequest request) {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

        if (!_battleSessionStore.TryGet(battleId, out var session)) {
            throw new KeyNotFoundException($"Battle '{battleId}' was not found.");
        }

        lock (session.SyncRoot) {
            var attacker = session.GetRequiredCreature(request.AttackerId);
            var target = session.GetRequiredCreature(request.TargetId);

            if (ReferenceEquals(attacker, target)) {
                throw new ArgumentException("Attacker and target must be different creatures.");
            }

            var attackMove = new DamageMove(request.Power, DamageKind.Physical) {
                Source = attacker,
                Destination = target
            };
            return ExecuteTurnWithCounter(session, attacker, attackMove);
        }
    }

    public BattleStateDto ExecuteAbility(string battleId, ExecuteAbilityRequest request)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        ArgumentNullException.ThrowIfNull(request);

        if (!_battleSessionStore.TryGet(battleId, out var session))
        {
            throw new KeyNotFoundException($"Battle '{battleId}' was not found.");
        }

        if (!AbilityCatalog.TryGetById(request.AbilityId, out var ability) || ability is null)
        {
            throw new ArgumentException($"Ability '{request.AbilityId}' does not exist.");
        }

        lock (session.SyncRoot)
        {
            var attacker = session.GetRequiredCreature(request.AttackerId);
            Creature? target = null;

            if (!string.IsNullOrWhiteSpace(request.TargetId))
            {
                target = session.GetRequiredCreature(request.TargetId);
            }

            if (ability.Target == DomainAbilityTarget.Enemy)
            {
                if (target is null)
                {
                    throw new ArgumentException("TargetId is required for enemy abilities.");
                }

                if (ReferenceEquals(attacker, target))
                {
                    throw new ArgumentException("Attacker and target must be different creatures.");
                }
            }
            else
            {
                if (target is not null && !ReferenceEquals(attacker, target))
                {
                    throw new ArgumentException("Self-targeted abilities must target the attacker.");
                }

                target = attacker;
            }

            var move = ability.CreateMove(attacker, target);
            return ExecuteTurnWithCounter(session, attacker, move);
        }
    }

    private static BattleStateDto MapState(BattleSession session) {
        var creature1Id = session.GetCreatureId(session.Battle.Creature1);
        var creature2Id = session.GetCreatureId(session.Battle.Creature2);
        var creature1 = MapCreature(session.Battle.Creature1, creature1Id);
        var creature2 = MapCreature(session.Battle.Creature2, creature2Id);
        var winnerId = session.Battle.TryGetResult(out var result)
            ? session.GetCreatureId(result!.Winner)
            : null;

        return new BattleStateDto(
            session.BattleId,
            session.RoundNumber,
            session.StateVersion,
            creature1,
            creature2,
            session.Battle.IsOver,
            winnerId);
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

    private BattleStateDto ExecuteTurnWithCounter(BattleSession session, Creature attacker, Move move)
    {
        var (counterAttacker, counterTarget) = GetCounterPair(session, attacker);
        var counterMove = CreateCounterMove(session, counterAttacker, counterTarget);

        _ = session.Battle.ExecuteTurn(move, counterMove);
        session.AdvanceRound();

        return MapState(session);
    }
}
