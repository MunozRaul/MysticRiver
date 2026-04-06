using Microsoft.AspNetCore.Mvc;
using MysticRiver.HttpApi.Infrastructure;
using MysticRiver.HttpApi.Models;

namespace MysticRiver.HttpApi.Controllers;

[ApiController]
[Route("api/match")]
// Accepts move submissions for a match and returns the updated move history.
public class MatchController : ControllerBase
{
    [HttpPost("{id}/move")]
    [ProducesResponseType<MoveResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public ActionResult<MoveResult> SubmitMove(string id, [FromBody] MoveRequest request)
    {
        var match = InMemoryMatchStore.Get(id);
        if (match is null)
        {
            return NotFound(new { message = $"Match '{id}' was not found." });
        }

        var playerExists = match.Players.Any(player =>
            string.Equals(player.Id, request.PlayerId, StringComparison.OrdinalIgnoreCase));

        if (!playerExists)
        {
            return BadRequest(new { message = $"Player '{request.PlayerId}' does not exist in match '{id}'." });
        }

        if (string.IsNullOrWhiteSpace(request.Move))
        {
            return BadRequest(new { message = "Move must not be empty." });
        }

        if (!string.IsNullOrWhiteSpace(match.CurrentTurnPlayerId) &&
            !string.Equals(match.CurrentTurnPlayerId, request.PlayerId, StringComparison.OrdinalIgnoreCase))
        {
            return Conflict(new { message = $"It is currently '{match.CurrentTurnPlayerId}' turn." });
        }

        var updatedMatch = InMemoryMatchStore.SubmitMove(id, request.PlayerId, request.Move.Trim());

        return Ok(new MoveResult
        {
            MatchId = updatedMatch.Id,
            Accepted = true,
            LastMove = request.Move.Trim(),
            NextPlayerId = updatedMatch.CurrentTurnPlayerId,
            Moves = updatedMatch.Moves
                .Select(move => new MoveResultEntry
                {
                    PlayerId = move.PlayerId,
                    Move = move.Move,
                    TimestampUtc = move.TimestampUtc
                })
                .ToArray()
        });
    }
}
