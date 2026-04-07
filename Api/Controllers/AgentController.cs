using Microsoft.AspNetCore.Mvc;
using SolitaAgent.Core.Contracts;
using SolitaAgent.Core.Services;

namespace SolitaAgent.Api.Controllers;

[ApiController]
[Route("api/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly IAgentOrchestrator _agentOrchestrator;

    public AgentController(IAgentOrchestrator agentOrchestrator)
    {
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
        if (string.IsNullOrWhiteSpace(question))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Question is required.",
                Detail = "Provide a non-empty 'question' query string parameter."
            });
        }

        var response = await _agentOrchestrator.AskAsync(question.Trim(), cancellationToken);
        return Ok(response);
    }
}
