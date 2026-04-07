// Comments are here to help the reviewer navigate the code.

using Microsoft.AspNetCore.Mvc;
using SolitaAgent.Api.Validators;
using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Services;

namespace SolitaAgent.Api.Controllers;

// Thin controller — no business logic. Sanitizes input then delegates to the orchestrator.
// Depends on interfaces only; concrete types are injected via DI.
[ApiController]
[Route("api/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly IInputSanitizer _inputSanitizer;
    private readonly IAgentOrchestrator _agentOrchestrator;

    public AgentController(
        IInputSanitizer inputSanitizer,
        IAgentOrchestrator agentOrchestrator)
    {
        _inputSanitizer = inputSanitizer;
        _agentOrchestrator = agentOrchestrator;
    }

    [HttpGet("ask")]
    [ProducesResponseType(typeof(AskResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<AskResponse>> Ask(
        [FromQuery] string? question,
        CancellationToken cancellationToken)
    {
        var sanitized = _inputSanitizer.Sanitize(question);
        var response = await _agentOrchestrator.AskAsync(sanitized, cancellationToken);
        return Ok(response);
    }
}
