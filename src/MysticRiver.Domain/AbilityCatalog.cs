namespace MysticRiver.Domain;

[Flags]
public enum AbilityTag
{
    Damage = 1,
    Heal = 2,
    Shield = 4,
    Status = 8,
    CrowdControl = 16,
    Lifesteal = 32,
}

public enum AbilityTarget
{
    Self,
    Enemy,
}

public sealed class AbilityDefinition
{
    private readonly Func<Creature, Creature?, Move> _createMove;

    public AbilityDefinition(
        string id,
        string name,
        AbilityTarget target,
        AbilityTag tags,
        int manaCost,
        Func<Creature, Creature?, Move> createMove)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Ability id is required.", nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Ability name is required.", nameof(name));
        }

        Id = id;
        Name = name;
        Target = target;
        Tags = tags;
        ManaCost = manaCost;
        _createMove = createMove ?? throw new ArgumentNullException(nameof(createMove));
    }

    public string Id { get; }
    public string Name { get; }
    public AbilityTarget Target { get; }
    public AbilityTag Tags { get; }
    public int ManaCost { get; }

    public Move CreateMove(Creature source, Creature? target)
    {
        ArgumentNullException.ThrowIfNull(source);

        if (Target == AbilityTarget.Enemy && target is null)
        {
            throw new ArgumentNullException(nameof(target));
        }

        return _createMove(source, target ?? source);
    }
}

public static class AbilityCatalog
{
    private static readonly IReadOnlyDictionary<string, AbilityDefinition> _definitions =
        new Dictionary<string, AbilityDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            ["basic-attack"] = new AbilityDefinition(
                "basic-attack",
                "Basic Attack",
                AbilityTarget.Enemy,
                AbilityTag.Damage,
                manaCost: 0,
                createMove: (source, target) => new DamageMove(20, DamageKind.Physical)
                {
                    Source = source,
                    Destination = target!,
                }),
            ["fireball"] = new AbilityDefinition(
                "fireball",
                "Fireball",
                AbilityTarget.Enemy,
                AbilityTag.Damage,
                manaCost: 12,
                createMove: (source, target) => new DamageMove(28, DamageKind.Magical)
                {
                    Source = source,
                    Destination = target!,
                    ManaCost = 12,
                }),
            ["healing-light"] = new AbilityDefinition(
                "healing-light",
                "Healing Light",
                AbilityTarget.Self,
                AbilityTag.Heal,
                manaCost: 10,
                createMove: (source, _) => new HealMove(20, 10)
                {
                    Self = source,
                }),
            ["shield-wall"] = new AbilityDefinition(
                "shield-wall",
                "Shield Wall",
                AbilityTarget.Self,
                AbilityTag.Shield,
                manaCost: 12,
                createMove: (source, _) => new ShieldMove(25, 12)
                {
                    Self = source,
                }),
            ["poison-strike"] = new AbilityDefinition(
                "poison-strike",
                "Poison Strike",
                AbilityTarget.Enemy,
                AbilityTag.Damage | AbilityTag.Status,
                manaCost: 6,
                createMove: (source, target) => new StatusDamageMove(12, DamageKind.Physical, StatusEffect.Poison)
                {
                    Source = source,
                    Destination = target!,
                    ManaCost = 6,
                }),
            ["bleed-strike"] = new AbilityDefinition(
                "bleed-strike",
                "Bleed Strike",
                AbilityTarget.Enemy,
                AbilityTag.Damage | AbilityTag.Status,
                manaCost: 6,
                createMove: (source, target) => new StatusDamageMove(12, DamageKind.Physical, StatusEffect.Bleed)
                {
                    Source = source,
                    Destination = target!,
                    ManaCost = 6,
                }),
            ["silence"] = new AbilityDefinition(
                "silence",
                "Silence",
                AbilityTarget.Enemy,
                AbilityTag.CrowdControl,
                manaCost: 8,
                createMove: (source, target) => new CrowdControlMove(2, CrowdControlKind.Silence)
                {
                    Source = source,
                    Destination = target!,
                    ManaCost = 8,
                }),
            ["stun"] = new AbilityDefinition(
                "stun",
                "Stun",
                AbilityTarget.Enemy,
                AbilityTag.CrowdControl,
                manaCost: 10,
                createMove: (source, target) => new CrowdControlMove(1, CrowdControlKind.Stun)
                {
                    Source = source,
                    Destination = target!,
                    ManaCost = 10,
                }),
            ["haste"] = new AbilityDefinition(
                "haste",
                "Haste",
                AbilityTarget.Self,
                AbilityTag.Status,
                manaCost: 6,
                createMove: (source, _) => new SelfStatusMove(StatusEffect.Haste)
                {
                    Self = source,
                    ManaCost = 6,
                }),
            ["slow"] = new AbilityDefinition(
                "slow",
                "Slow",
                AbilityTarget.Enemy,
                AbilityTag.Status,
                manaCost: 6,
                createMove: (source, target) => new StatusEffectMove(StatusEffect.Slow)
                {
                    Source = source,
                    Destination = target!,
                    ManaCost = 6,
                }),
            ["lifesteal"] = new AbilityDefinition(
                "lifesteal",
                "Lifesteal",
                AbilityTarget.Enemy,
                AbilityTag.Damage | AbilityTag.Lifesteal,
                manaCost: 8,
                createMove: (source, target) => new LifestealMove(15, DamageKind.Physical, 0.5)
                {
                    Source = source,
                    Destination = target!,
                    ManaCost = 8,
                }),
        };

    private static readonly IReadOnlyCollection<AbilityDefinition> _allDefinitions =
        new List<AbilityDefinition>(_definitions.Values);

    public static IReadOnlyCollection<AbilityDefinition> All => _allDefinitions;

    public static bool TryGetById(string id, out AbilityDefinition? definition)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            definition = null;
            return false;
        }

        return _definitions.TryGetValue(id, out definition);
    }
}
