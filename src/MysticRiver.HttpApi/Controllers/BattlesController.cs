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
    IHubContext<BattleHub, IBattleClient> battleHubContext,
    ILogger<BattlesController> logger) : ControllerBase {
    private readonly IBattleService _battleService = battleService;
    private readonly IHubContext<BattleHub, IBattleClient> _battleHubContext = battleHubContext;
    private readonly ILogger<BattlesController> _logger = logger;

    [HttpPost("start")]
    public ActionResult<StartBattleResponse> StartBattle([FromBody] StartBattleRequest request) {
        try {
            _logger.LogInformation("Battle started: {PlayerName} vs {EnemyName}", request.PlayerName, request.EnemyName);
            var response = _battleService.StartBattle(request);
            _logger.LogInformation("Battle {BattleId} created successfully", response.BattleId);
            return Ok(response);
        }
        catch (ArgumentException exception) {
            _logger.LogWarning("Battle start failed: {Reason}", exception.Message);
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
            _logger.LogWarning("Battle {BattleId} not found", battleId);
            return NotFound(CreateProblem("Battle not found.", exception.Message));
        }
    }

    [HttpGet("abilities")]
    public ActionResult<IReadOnlyList<AbilityDefinitionDto>> GetAbilities()
    {
        var abilities = _battleService.GetAbilities();
        return Ok(abilities);
    }

    /// <summary>
    /// Executes a basic attack in the battle.
    /// This is a convenience endpoint that delegates to ExecuteAbility with "basic-attack" as the ability ID.
    /// For custom abilities or future flexibility, use the /ability endpoint instead.
    /// </summary>
    [HttpPost("{battleId}/actions/basic-attack")]
    public async Task<ActionResult<BattleStateDto>> ExecuteBasicAttack(string battleId, [FromBody] ExecuteBasicAttackRequest request) {
        return await ExecuteBattleActionAsync(
            battleId,
            () => _battleService.ExecuteBasicAttack(battleId, request),
            "Invalid attack request.",
            "basic attack");
    }

    /// <summary>
    /// Executes any ability (including basic-attack) by ID.
    /// This is the generic endpoint for all move types; use for custom abilities or flexibility.
    /// The AbilityCatalog defines all available abilities with their properties (mana cost, target type, etc.).
    /// </summary>
    [HttpPost("{battleId}/actions/ability")]
    public async Task<ActionResult<BattleStateDto>> ExecuteAbility(string battleId, [FromBody] ExecuteAbilityRequest request)
{
    return await ExecuteBattleActionAsync(
        battleId,
        () => _battleService.ExecuteAbility(battleId, request),
        "Invalid ability request.",
        $"ability {request.AbilityId}");
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
    string invalidRequestTitle,
    string actionType)
{
    try
    {
        var result = action();
        _logger.LogInformation("Battle {BattleId}: {ActionType} executed at round {Round}", battleId, actionType, result.State.RoundNumber);
        var battleEvent = new BattleStateUpdatedEvent(battleId, result.State, result.ActionSummary);

        await _battleHubContext.Clients.Group(battleId).BattleStateUpdated(battleEvent);
        return Ok(result.State);
    }
    catch (KeyNotFoundException exception)
    {
        _logger.LogWarning("Battle {BattleId}: {ActionType} failed - not found: {Reason}", battleId, actionType, exception.Message);
        return NotFound(CreateProblem("Battle or creature not found.", exception.Message));
    }
    catch (InvalidOperationException exception)
    {
        _logger.LogWarning("Battle {BattleId}: {ActionType} cannot be executed - {Reason}", battleId, actionType, exception.Message);
        return BadRequest(CreateProblem("Battle action cannot be executed.", exception.Message));
    }
    catch (ArgumentException exception)
    {
        _logger.LogWarning("Battle {BattleId}: {ActionType} invalid request - {Reason}", battleId, actionType, exception.Message);
        return BadRequest(CreateProblem(invalidRequestTitle, exception.Message));
    }
}

}
