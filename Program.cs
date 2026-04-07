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
builder.Services.AddSingleton<IGeminiAgentClient, GeminiAgentClient>();
builder.Services.AddSingleton<IKnowledgeSnippetRepository, InMemoryKnowledgeSnippetRepository>();
builder.Services.AddSingleton<SimpleTextVectorizer>();
builder.Services.AddSingleton<IVectorKnowledgeTool, VectorKnowledgeTool>();
builder.Services.AddSingleton<IStaticResponseTool, StaticResponseTool>();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthorization();
app.MapControllers();

app.Run();
