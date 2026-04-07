// Comments are here to help the reviewer navigate the code.

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SolitaAgent.Api.Exceptions;
using SolitaAgent.Core.Exceptions;

namespace SolitaAgent.Api.Configuration;

// Middleware pipeline extracted from Program.cs.
// The global exception handler maps domain exceptions to RFC 7807 ProblemDetails responses.
public static class WebApplicationExtensions
{
    public static WebApplication UseSolitaAgentMiddleware(this WebApplication app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
                var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
                var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

                var (statusCode, title, detail) = exception switch
                {
                    InputValidationException ex => (
                        StatusCodes.Status400BadRequest,
                        "Invalid input.",
                        ex.Message),
                    LlmProviderException { Kind: LlmErrorKind.ApiKeyMissing } => (
                        StatusCodes.Status503ServiceUnavailable,
                        "LLM API key is missing.",
                        "Set the required API key environment variable before calling this endpoint."),
                    LlmProviderException { Kind: LlmErrorKind.ProviderUnavailable } => (
                        StatusCodes.Status503ServiceUnavailable,
                        "LLM provider is unavailable.",
                        "The configured LLM provider could not complete the request."),
                    LlmProviderException { Kind: LlmErrorKind.ProviderRejected } => (
                        StatusCodes.Status502BadGateway,
                        "LLM provider request failed.",
                        "The configured LLM provider rejected the request."),
                    _ => (
                        StatusCodes.Status500InternalServerError,
                        "An unexpected error occurred.",
                        "The server could not process the request.")
                };

                if (exception is not null)
                {
                    logger.LogError(exception, "Request failed with status code {StatusCode}.", statusCode);
                }

                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/problem+json";

                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = detail
                });
            });
        });

        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
