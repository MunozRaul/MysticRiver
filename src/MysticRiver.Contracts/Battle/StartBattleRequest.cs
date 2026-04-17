namespace MysticRiver.Contracts.Battle;

public sealed record StartBattleRequest(
    string PlayerName = "Knight of the River",
    string EnemyName = "Wraith Duelist",
    int PlayerMaxHp = 120,
    int EnemyMaxHp = 110,
    int PlayerInitiative = 15,
    int EnemyInitiative = 10,
    int EnemyAttackPower = 12);
