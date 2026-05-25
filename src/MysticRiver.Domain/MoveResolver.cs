namespace MysticRiver.Domain;

public interface IMoveResolver
{
    bool IsManaMove(Move move);
    void Resolve(Move move);
}

public sealed class DefaultMoveResolver : IMoveResolver
{
    public bool IsManaMove(Move move) =>
        move is HealMove or ShieldMove or ManaBurnMove or ManaDrainMove
            or DamageMove { ManaCost: > 0 }
            or StatusDamageMove { ManaCost: > 0 }
            or CrowdControlMove { ManaCost: > 0 }
            or StatusEffectMove { ManaCost: > 0 }
            or SelfStatusMove { ManaCost: > 0 }
            or LifestealMove { ManaCost: > 0 };

    public void Resolve(Move move)
    {
        switch (move)
        {
            case DamageMove dm:
                if (dm.ManaCost == 0 || dm.Source.TryConsumeMana(dm.ManaCost))
                {
                    dm.Destination.TakeDamage(dm.DamageAmount, dm.Kind);
                }
                break;

            case HealMove hm:
                if (hm.Self.TryConsumeMana(hm.ManaCost))
                {
                    hm.Self.Heal(hm.HealAmount);
                }
                break;

            case ShieldMove sm:
                if (sm.Self.TryConsumeMana(sm.ManaCost))
                {
                    sm.Self.ApplyShield(sm.ShieldAmount);
                }
                break;

            case ManaRestoreMove mrm:
                mrm.Self.RestoreMana(mrm.ManaAmount);
                break;

            case ManaBurnMove mbm:
                mbm.Self.TryConsumeMana(mbm.ManaAmount);
                break;

            case ManaDrainMove mdm:
                mdm.Destination.TryConsumeMana(mdm.ManaAmount);
                break;

            case ResistanceShredMove rsm:
                if (rsm.Kind == DamageKind.Physical)
                {
                    rsm.Destination.PhysicalResistance =
                        Math.Max(0, rsm.Destination.PhysicalResistance - rsm.FlatShred);
                }
                else
                {
                    rsm.Destination.MagicalResistance =
                        Math.Max(0, rsm.Destination.MagicalResistance - rsm.FlatShred);
                }
                break;

            case StatusDamageMove sdm:
                if (sdm.ManaCost == 0 || sdm.Source.TryConsumeMana(sdm.ManaCost))
                {
                    sdm.Destination.TakeDamage(sdm.DamageAmount, sdm.Kind);
                    if (!sdm.Destination.IsDead)
                    {
                        sdm.Destination.ApplyStatus(sdm.Effect);
                    }
                }
                break;

            case CrowdControlMove ccm:
                if (ccm.ManaCost == 0 || ccm.Source.TryConsumeMana(ccm.ManaCost))
                {
                    ccm.Destination.ApplyCrowdControl(ccm.CrowdControlType, ccm.Turns);
                }
                break;

            case LifestealMove lsm:
                if (lsm.ManaCost == 0 || lsm.Source.TryConsumeMana(lsm.ManaCost))
                {
                    var initialHp = lsm.Destination.CurrentHp;
                    lsm.Destination.TakeDamage(lsm.DamageAmount, lsm.Kind);
                    var damageDealt = initialHp - lsm.Destination.CurrentHp;
                    if (damageDealt > 0)
                    {
                        var healAmount = (int)Math.Round(damageDealt * lsm.HealRatio, MidpointRounding.AwayFromZero);
                        lsm.Source.Heal(healAmount);
                    }
                }
                break;

            case StatusEffectMove sem:
                if (sem.ManaCost == 0 || sem.Source.TryConsumeMana(sem.ManaCost))
                {
                    if (!sem.Destination.IsDead)
                    {
                        sem.Destination.ApplyStatus(sem.Effect);
                    }
                }
                break;

            case SelfStatusMove ssm:
                if (ssm.ManaCost == 0 || ssm.Self.TryConsumeMana(ssm.ManaCost))
                {
                    if (!ssm.Self.IsDead)
                    {
                        ssm.Self.ApplyStatus(ssm.Effect);
                    }
                }
                break;

            default:
                throw new ArgumentException($"Unhandled move type: {move.GetType().Name}");
        }
    }
}
