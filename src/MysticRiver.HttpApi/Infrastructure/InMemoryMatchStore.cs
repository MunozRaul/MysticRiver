using MysticRiver.HttpApi.Domain;

namespace MysticRiver.HttpApi.Infrastructure;

// Provides a temporary in-memory store for demo matches and submitted moves.
public static class InMemoryMatchStore
{
    private static readonly Lock SyncRoot = new();
    private static readonly Dictionary<string, Match> Matches = new(StringComparer.OrdinalIgnoreCase)
    {
        ["match-1"] = new Match
        {
            Id = "match-1",
            Players =
            [
                new Player { Id = "player-1", Name = "Player 1" },
                new Player { Id = "player-2", Name = "Player 2" }
            ],
            Moves = [],
            CurrentTurnPlayerId = "player-1"
        }
    };

    public static Match? Get(string matchId)
    {
        lock (SyncRoot)
        {
            return Matches.TryGetValue(matchId, out var match) ? match : null;
        }
    }

    public static Match SubmitMove(string matchId, string playerId, string move)
    {
        lock (SyncRoot)
        {
            var match = Matches[matchId];

            var entry = new MoveEntry
            {
                PlayerId = playerId,
                Move = move,
                TimestampUtc = DateTime.UtcNow
            };

            match.Moves.Add(entry);
            match.LastMoveAtUtc = entry.TimestampUtc;
            match.CurrentTurnPlayerId = GetNextPlayerId(match, playerId);

            return match;
        }
    }

    private static string? GetNextPlayerId(Match match, string currentPlayerId)
    {
        if (match.Players.Count <= 1)
        {
            return currentPlayerId;
        }

        var currentIndex = match.Players.FindIndex(player =>
            string.Equals(player.Id, currentPlayerId, StringComparison.OrdinalIgnoreCase));

        if (currentIndex < 0)
        {
            return match.CurrentTurnPlayerId;
        }

        var nextIndex = (currentIndex + 1) % match.Players.Count;
        return match.Players[nextIndex].Id;
    }
}
