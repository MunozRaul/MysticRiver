using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using MysticRiver.Contracts.Battle;
using MysticRiver.HttpApi.Battles;

namespace MysticRiver.HttpApi.Controllers;

[ApiController]
[Route("api/battles")]
public sealed class BattlesController(
    IBattleService battleService,
    IHubContext<BattleHub, IBattleClient> battleHubContext) : ControllerBase
{
    private readonly IBattleService _battleService = battleService;
    private readonly IHubContext<BattleHub, IBattleClient> _battleHubContext = battleHubContext;

    [HttpPost("start")]
    public ActionResult<StartBattleResponse> StartBattle([FromBody] StartBattleRequest request)
    {
        try
        {
            var response = _battleService.StartBattle(request);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblem("Invalid battle setup.", exception.Message));
        }
    }

    [HttpPost("{battleId}/actions/basic-attack")]
    public async Task<ActionResult<BattleStateDto>> ExecuteBasicAttack(string battleId, [FromBody] ExecuteBasicAttackRequest request)
    {
        try
        {
            var state = _battleService.ExecuteBasicAttack(battleId, request);
            var battleEvent = new BattleStateUpdatedEvent(battleId, state);

            await _battleHubContext.Clients.Group(battleId).BattleStateUpdated(battleEvent);
            return Ok(state);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(CreateProblem("Battle or creature not found.", exception.Message));
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(CreateProblem("Battle action cannot be executed.", exception.Message));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(CreateProblem("Invalid attack request.", exception.Message));
        }
    }

    private static ProblemDetails CreateProblem(string title, string detail)
    {
        return new ProblemDetails
        {
            Title = title,
            Detail = detail
        };
    }
}
