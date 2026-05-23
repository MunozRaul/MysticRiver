using System;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

using MysticRiver.Application.Battles;
using MysticRiver.Contracts.Battle;
using MysticRiver.HttpApi.Battles;

namespace MysticRiver.HttpApi.Controllers;

[ApiController]
[Route("api/battles")]
public sealed class BattlesController(
    IBattleService battleService,
    IHubContext<BattleHub, IBattleClient> battleHubContext) : ControllerBase {
    private readonly IBattleService _battleService = battleService;
    private readonly IHubContext<BattleHub, IBattleClient> _battleHubContext = battleHubContext;

    [HttpPost("start")]
    public ActionResult<StartBattleResponse> StartBattle([FromBody] StartBattleRequest request) {
        try {
            var response = _battleService.StartBattle(request);
            return Ok(response);
        }
        catch (ArgumentException exception) {
            return BadRequest(CreateProblem("Invalid battle setup.", exception.Message));
        }
    }

    [HttpGet("{battleId}")]
    public ActionResult<BattleStateDto> GetBattleState(string battleId)
    {
        try
        {
            var state = _battleService.GetBattleState(battleId);
            return Ok(state);
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(CreateProblem("Battle not found.", exception.Message));
        }
    }

    [HttpGet("abilities")]
    public ActionResult<IReadOnlyList<AbilityDefinitionDto>> GetAbilities()
    {
        var abilities = _battleService.GetAbilities();
        return Ok(abilities);
    }

[HttpPost("{battleId}/actions/basic-attack")]
public async Task<ActionResult<BattleStateDto>> ExecuteBasicAttack(string battleId, [FromBody] ExecuteBasicAttackRequest request) {
    return await ExecuteBattleActionAsync(
        battleId,
        () => _battleService.ExecuteBasicAttack(battleId, request),
        "Invalid attack request.");
}

[HttpPost("{battleId}/actions/ability")]
public async Task<ActionResult<BattleStateDto>> ExecuteAbility(string battleId, [FromBody] ExecuteAbilityRequest request)
{
    return await ExecuteBattleActionAsync(
        battleId,
        () => _battleService.ExecuteAbility(battleId, request),
        "Invalid ability request.");
}

private static ProblemDetails CreateProblem(string title, string detail) {
    return new ProblemDetails {
        Title = title,
        Detail = detail
    };
}

private async Task<ActionResult<BattleStateDto>> ExecuteBattleActionAsync(
    string battleId,
    Func<BattleActionResult> action,
    string invalidRequestTitle)
{
    try
    {
        var result = action();
        var battleEvent = new BattleStateUpdatedEvent(battleId, result.State, result.ActionSummary);

        await _battleHubContext.Clients.Group(battleId).BattleStateUpdated(battleEvent);
        return Ok(result.State);
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
        return BadRequest(CreateProblem(invalidRequestTitle, exception.Message));
    }
}

}
