namespace MysticRiver.Contracts.Battle;

public sealed record CreateMatchRequest(
    string HostPlayerId,
    string HostDisplayName = "Knight of the River",
    string OpponentDisplayName = "Wraith Duelist",
    int HostMaxHp = 120,
    int OpponentMaxHp = 110,
    int HostInitiative = 15,
    int OpponentInitiative = 20,
    int OpponentAttackPower = 12);
