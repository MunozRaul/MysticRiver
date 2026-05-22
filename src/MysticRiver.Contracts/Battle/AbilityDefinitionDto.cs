namespace MysticRiver.Contracts.Battle;

public sealed record AbilityDefinitionDto(
    string Id,
    string Name,
    AbilityTarget Target,
    AbilityTag Tags,
    int ManaCost);
