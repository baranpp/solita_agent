using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SolitaAgent.Api.Configuration;
using SolitaAgent.Api.Exceptions;
using SolitaAgent.Api.Validators;
using SolitaAgent.Core.Exceptions;
using SolitaAgent.Core.Options;
using SolitaAgent.Core.Services;
using SolitaAgent.Infrastructure.LlmProviders.Gemini;
using SolitaAgent.Infrastructure.LlmProviders.Groq;
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

builder.Services.AddSingleton<IInputSanitizer, InputSanitizationValidator>();
builder.Services.AddSingleton<IQuestionValidator, QuestionHeuristicValidator>();
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
builder.Services.AddSingleton<ITextVectorizer, SimpleTextVectorizer>();
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
            InputValidationException ex => (
                StatusCodes.Status400BadRequest,
                "Invalid input.",
                ex.Message),
            QuestionValidationException ex => (
                StatusCodes.Status400BadRequest,
                "Invalid question format.",
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

app.Run();
