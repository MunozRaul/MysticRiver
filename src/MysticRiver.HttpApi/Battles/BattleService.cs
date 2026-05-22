using MysticRiver.Contracts.Battle;
using MysticRiver.Domain;
using ContractCrowdControlKind = MysticRiver.Contracts.Battle.CrowdControlKind;
using ContractStatusEffect = MysticRiver.Contracts.Battle.StatusEffect;
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

            var counterAttacker = ReferenceEquals(attacker, session.Battle.Creature1)
                ? session.Battle.Creature2
                : session.Battle.Creature1;
            var counterTarget = attacker;

            var attackMove = new DamageMove(request.Power, DamageKind.Physical) {
                Source = attacker,
                Destination = target
            };
            var counterMove = new DamageMove(session.EnemyAttackPower, DamageKind.Physical) {
                Source = counterAttacker,
                Destination = counterTarget
            };

            _ = session.Battle.ExecuteTurn(attackMove, counterMove);
            session.AdvanceRound();

            return MapState(session);
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
}
