using Google.GenAI;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SolitaAgent.Api.Configuration;
using SolitaAgent.Core.Options;
using SolitaAgent.Core.Services;
using SolitaAgent.Infrastructure.LlmProviders.Gemini;
using SolitaAgent.Infrastructure.LlmProviders.Gemini.Exceptions;
using SolitaAgent.Infrastructure.LlmProviders.Groq;
using SolitaAgent.Infrastructure.LlmProviders.Groq.Exceptions;
using SolitaAgent.Infrastructure.Repositories;
using SolitaAgent.Infrastructure.Tools;

EnvFileLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var llmProvider = Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "groq";

builder.Services.Configure<GeminiOptions>(options =>
{
    builder.Configuration.GetSection(GeminiOptions.SectionName).Bind(options);
    options.ApiKey = builder.Configuration["GEMINI_API_KEY"];
    options.Model = string.IsNullOrWhiteSpace(options.Model)
        ? GeminiOptions.DefaultModel
        : options.Model;
});

builder.Services.Configure<GroqOptions>(options =>
{
    builder.Configuration.GetSection(GroqOptions.SectionName).Bind(options);
    options.ApiKey = builder.Configuration["GROQ_API_KEY"];
    options.Model = string.IsNullOrWhiteSpace(options.Model)
        ? GroqOptions.DefaultModel
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

if (string.Equals(llmProvider, "gemini", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddSingleton<GeminiClient>();
    builder.Services.AddSingleton<IToolSelectionClient>(sp => sp.GetRequiredService<GeminiClient>());
    builder.Services.AddSingleton<IAnswerGenerationClient>(sp => sp.GetRequiredService<GeminiClient>());
}
else
{
    builder.Services.AddHttpClient("Groq", client =>
    {
        client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
    });
    builder.Services.AddSingleton<GroqClient>();
    builder.Services.AddSingleton<IToolSelectionClient>(sp => sp.GetRequiredService<GroqClient>());
    builder.Services.AddSingleton<IAnswerGenerationClient>(sp => sp.GetRequiredService<GroqClient>());
}

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
                "LLM API key is missing.",
                "Set the GEMINI_API_KEY environment variable before calling this endpoint."),
            MissingGroqApiKeyException => (
                StatusCodes.Status503ServiceUnavailable,
                "LLM API key is missing.",
                "Set the GROQ_API_KEY environment variable before calling this endpoint."),
            ClientError => (
                StatusCodes.Status502BadGateway,
                "LLM provider request failed.",
                "The configured LLM provider rejected the request."),
            ServerError => (
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The configured LLM provider could not complete the request."),
            GroqApiException groqEx when (int?)groqEx.StatusCode >= 500 => (
                StatusCodes.Status503ServiceUnavailable,
                "LLM provider is unavailable.",
                "The configured LLM provider could not complete the request."),
            GroqApiException => (
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

app.Run();
