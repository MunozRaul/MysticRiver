using System;
using System.Linq;

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
    IConnectionMapping connectionMapping,
    ILogger<BattlesController> logger) : ControllerBase {
    private readonly IBattleService _battleService = battleService;
    private readonly IHubContext<BattleHub, IBattleClient> _battleHubContext = battleHubContext;
    private readonly IConnectionMapping _connectionMapping = connectionMapping;
    private readonly ILogger<BattlesController> _logger = logger;

    [HttpPost("matches/create")]
    public ActionResult<CreateMatchResponse> CreateMatch([FromBody] CreateMatchRequest request) {
        try {
            var response = _battleService.CreateMatch(request);
            _logger.LogInformation("Match {BattleId} created for host {HostPlayerId}", response.BattleId, response.HostPlayerId);
            return Ok(response);
        }
        catch (ArgumentException exception) {
            _logger.LogWarning("Match creation failed: {Reason}", exception.Message);
            return BadRequest(CreateProblem("Invalid match setup.", exception.Message));
        }
    }

    [HttpPost("{battleId}/matches/join")]
    public ActionResult<JoinMatchResponse> JoinMatch(string battleId, [FromBody] JoinMatchRequest request) {
        try {
            var response = _battleService.JoinMatch(battleId, request);
            _logger.LogInformation("Match {BattleId} joined by guest {GuestPlayerId}", response.BattleId, response.GuestPlayerId);
            return Ok(response);
        }
        catch (KeyNotFoundException exception) {
            _logger.LogWarning("Join match failed for battle {BattleId}: {Reason}", battleId, exception.Message);
            return NotFound(CreateProblem("Match not found.", exception.Message));
        }
        catch (InvalidOperationException exception) {
            _logger.LogWarning("Join match failed for battle {BattleId}: {Reason}", battleId, exception.Message);
            return BadRequest(CreateProblem("Match cannot be joined.", exception.Message));
        }
        catch (ArgumentException exception) {
            _logger.LogWarning("Join match failed for battle {BattleId}: {Reason}", battleId, exception.Message);
            return BadRequest(CreateProblem("Invalid join request.", exception.Message));
        }
    }

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

    [HttpPost("{battleId}/abandon")]
    public async Task<ActionResult<BattleStateDto>> AbandonBattle(string battleId, [FromBody] AbandonBattleRequest request) {
        bool requiresToken;
        try {
            requiresToken = _battleService.RequiresPlayerToken(battleId);
        }
        catch (KeyNotFoundException exception) {
            _logger.LogWarning("Battle {BattleId} not found for abandon", battleId);
            return NotFound(CreateProblem("Battle not found.", exception.Message));
        }

        if (!requiresToken) {
            return await ExecuteBattleActionAsync(
                battleId,
                () => _battleService.AbandonBattle(battleId, request),
                "Invalid abandon request.",
                "abandon");
        }

        // Validate token from header maps to this battle and player
        if (!Request.Headers.TryGetValue("X-Player-Token", out var tokenValues)) {
            _logger.LogWarning("Missing player token for abandon on battle {BattleId}", battleId);
            return StatusCode(403, CreateProblem("Unauthorized", "Missing player token."));
        }
        var token = tokenValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token) || !_connectionMapping.TryGetByToken(token, out var tokBattle, out var tokPlayer, out var tokDisplayName) || tokBattle != battleId) {
            _logger.LogWarning("Invalid or expired token for abandon on battle {BattleId}", battleId);
            return StatusCode(403, CreateProblem("Unauthorized", "Invalid or expired player token."));
        }
        if (tokPlayer != request.AbandoningCreatureId) {
            _logger.LogWarning("Token player mismatch for abandon on battle {BattleId}: tokenPlayer={TokenPlayer} requestPlayer={RequestPlayer}", battleId, tokPlayer, request.AbandoningCreatureId);
            return StatusCode(403, CreateProblem("Unauthorized", "Token does not own the abandoning creature."));
        }

        return await ExecuteBattleActionAsync(
            battleId,
            () => _battleService.AbandonBattle(battleId, request),
            "Invalid abandon request.",
            "abandon");
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
        bool requiresToken;
        try {
            requiresToken = _battleService.RequiresPlayerToken(battleId);
        }
        catch (KeyNotFoundException exception) {
            _logger.LogWarning("Battle {BattleId} not found for basic-attack", battleId);
            return NotFound(CreateProblem("Battle not found.", exception.Message));
        }

        if (!requiresToken) {
            return await ExecuteBattleActionAsync(
                battleId,
                () => _battleService.ExecuteBasicAttack(battleId, request),
                "Invalid attack request.",
                "basic attack");
        }

        // Validate token header maps to this battle and attacker
        if (!Request.Headers.TryGetValue("X-Player-Token", out var tokenValues)) {
            _logger.LogWarning("Missing player token for basic-attack on battle {BattleId}", battleId);
            return StatusCode(403, CreateProblem("Unauthorized", "Missing player token."));
        }
        var token = tokenValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token) || !_connectionMapping.TryGetByToken(token, out var tokBattle, out var tokPlayer, out var tokDisplayName) || tokBattle != battleId) {
            _logger.LogWarning("Invalid or expired token for basic-attack on battle {BattleId}", battleId);
            return StatusCode(403, CreateProblem("Unauthorized", "Invalid or expired player token."));
        }
        if (tokPlayer != request.AttackerId) {
            _logger.LogWarning("Token player mismatch for basic-attack on battle {BattleId}: tokenPlayer={TokenPlayer} requestPlayer={RequestPlayer}", battleId, tokPlayer, request.AttackerId);
            return StatusCode(403, CreateProblem("Unauthorized", "Token does not own the attacker creature."));
        }

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
        bool requiresToken;
        try {
            requiresToken = _battleService.RequiresPlayerToken(battleId);
        }
        catch (KeyNotFoundException exception) {
            _logger.LogWarning("Battle {BattleId} not found for ability execution", battleId);
            return NotFound(CreateProblem("Battle not found.", exception.Message));
        }

        if (!requiresToken) {
            return await ExecuteBattleActionAsync(
                battleId,
                () => _battleService.ExecuteAbility(battleId, request),
                "Invalid ability request.",
                $"ability {request.AbilityId}");
        }

        // Validate token header maps to this battle and attacker
        if (!Request.Headers.TryGetValue("X-Player-Token", out var tokenValues)) {
            _logger.LogWarning("Missing player token for ability on battle {BattleId}", battleId);
            return StatusCode(403, CreateProblem("Unauthorized", "Missing player token."));
        }
        var token = tokenValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(token) || !_connectionMapping.TryGetByToken(token, out var tokBattle, out var tokPlayer, out var tokDisplayName) || tokBattle != battleId) {
            _logger.LogWarning("Invalid or expired token for ability on battle {BattleId}", battleId);
            return StatusCode(403, CreateProblem("Unauthorized", "Invalid or expired player token."));
        }
        if (tokPlayer != request.AttackerId) {
            _logger.LogWarning("Token player mismatch for ability on battle {BattleId}: tokenPlayer={TokenPlayer} requestPlayer={RequestPlayer}", battleId, tokPlayer, request.AttackerId);
            return StatusCode(403, CreateProblem("Unauthorized", "Token does not own the attacker creature."));
        }

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
            var battleEvent = new BattleStateUpdatedEvent(battleId, result.State, result.ActionSummaries);

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
