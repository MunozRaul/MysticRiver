using Microsoft.AspNetCore.SignalR;

namespace MysticRiver.HttpApi.Battles;

public sealed class BattleHub : Hub<IBattleClient>
{
    public Task JoinBattle(string battleId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(battleId);
        return Groups.AddToGroupAsync(Context.ConnectionId, battleId);
    }
}
