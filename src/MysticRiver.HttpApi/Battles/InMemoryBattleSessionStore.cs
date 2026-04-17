using System.Collections.Concurrent;

using MysticRiver.Contracts.Battle;
using MysticRiver.Domain;

namespace MysticRiver.HttpApi.Battles;

public sealed class InMemoryBattleSessionStore : IBattleSessionStore
{
    private readonly ConcurrentDictionary<string, BattleSession> _sessions = new(StringComparer.OrdinalIgnoreCase);

    public BattleSession Create(StartBattleRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var battleId = Guid.NewGuid().ToString("N");
        var player = new Creature(request.PlayerName, request.PlayerMaxHp, request.PlayerInitiative);
        var enemy = new Creature(request.EnemyName, request.EnemyMaxHp, request.EnemyInitiative);
        var battle = new Battle(player, enemy);
        var session = new BattleSession(battleId, battle, request.EnemyAttackPower);

        if (_sessions.TryAdd(battleId, session))
        {
            return session;
        }

        throw new InvalidOperationException("Failed to create a new battle session.");
    }

    public bool TryGet(string battleId, out BattleSession session)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        return _sessions.TryGetValue(battleId, out session!);
    }
}
