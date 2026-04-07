using Google.GenAI;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SolitaAgent.Configuration;
using SolitaAgent.Options;
using SolitaAgent.Repositories;
using SolitaAgent.Services;
using SolitaAgent.Tools;

EnvFileLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<GeminiOptions>(options =>
{
    builder.Configuration.GetSection(GeminiOptions.SectionName).Bind(options);
    options.ApiKey = builder.Configuration["GEMINI_API_KEY"];
    options.Model = string.IsNullOrWhiteSpace(options.Model)
        ? GeminiOptions.DefaultModel
        : options.Model;
});

builder.Services.Configure<VectorSearchOptions>(options =>
{
    builder.Configuration.GetSection(VectorSearchOptions.SectionName).Bind(options);
    if (options.SimilarityThreshold <= 0 || options.SimilarityThreshold > 1)
    {
        options.SimilarityThreshold = VectorSearchOptions.DefaultSimilarityThreshold;
    }
});

builder.Services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();
builder.Services.AddSingleton<IToolSelectionClient, GeminiToolSelectionClient>();
builder.Services.AddSingleton<IKnowledgeSnippetRepository, InMemoryKnowledgeSnippetRepository>();
builder.Services.AddSingleton<SimpleTextVectorizer>();
builder.Services.AddSingleton<IVectorKnowledgeTool, VectorKnowledgeTool>();
builder.Services.AddSingleton<IStaticResponseTool, StaticResponseTool>();

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("GlobalExceptionHandler");
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

        var (statusCode, title, detail) = exception switch
        {
            MissingGeminiApiKeyException => (
                StatusCodes.Status503ServiceUnavailable,
                "Gemini API key is missing.",
                "Set the GEMINI_API_KEY environment variable before calling this endpoint."),
            ClientError => (
                StatusCodes.Status502BadGateway,
                "LLM provider request failed.",
                "The configured LLM provider rejected the request."),
            ServerError => (
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The configured LLM provider could not complete the request."),
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

app.Run();
