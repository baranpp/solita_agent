// Comments are here to help the reviewer navigate the code.

using SolitaAgent.Api.Validators;
using SolitaAgent.Core.Options;
using SolitaAgent.Core.Services;
using SolitaAgent.Infrastructure.LlmProviders.Gemini;
using SolitaAgent.Infrastructure.LlmProviders.Groq;
using SolitaAgent.Infrastructure.Repositories;
using SolitaAgent.Infrastructure.Tools;

namespace SolitaAgent.Api.Configuration;

// Composition root logic extracted from Program.cs for readability.
// Registers options, core services, the selected LLM provider, and infrastructure services.
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSolitaAgent(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GeminiOptions>(options =>
        {
            configuration.GetSection(GeminiOptions.SectionName).Bind(options);
            options.ApiKey = configuration["GEMINI_API_KEY"];
            options.Model = string.IsNullOrWhiteSpace(options.Model)
                ? GeminiOptions.DefaultModel
                : options.Model;
        });

        services.Configure<GroqOptions>(options =>
        {
            configuration.GetSection(GroqOptions.SectionName).Bind(options);
            options.ApiKey = configuration["GROQ_API_KEY"];
            options.Model = string.IsNullOrWhiteSpace(options.Model)
                ? GroqOptions.DefaultModel
                : options.Model;
        });

        services.Configure<VectorSearchOptions>(options =>
        {
            configuration.GetSection(VectorSearchOptions.SectionName).Bind(options);
            if (options.SimilarityThreshold <= 0 || options.SimilarityThreshold > 1)
            {
                options.SimilarityThreshold = VectorSearchOptions.DefaultSimilarityThreshold;
            }
        });

        services.AddSingleton<IInputSanitizer, InputSanitizationValidator>();
        services.AddSingleton<IAgentOrchestrator, AgentOrchestrator>();

        // Both Gemini and Groq implement the same Core interfaces (IToolSelectionClient,
        // IAnswerGenerationClient), so the rest of the application is provider-agnostic.
        var llmProvider = Environment.GetEnvironmentVariable("LLM_PROVIDER") ?? "groq";

        if (string.Equals(llmProvider, "gemini", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<GeminiClient>();
            services.AddSingleton<IToolSelectionClient>(sp => sp.GetRequiredService<GeminiClient>());
            services.AddSingleton<IAnswerGenerationClient>(sp => sp.GetRequiredService<GeminiClient>());
        }
        else
        {
            services.AddHttpClient("Groq", client =>
            {
                client.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            services.AddSingleton<GroqClient>();
            services.AddSingleton<IToolSelectionClient>(sp => sp.GetRequiredService<GroqClient>());
            services.AddSingleton<IAnswerGenerationClient>(sp => sp.GetRequiredService<GroqClient>());
        }

        services.AddSingleton<IKnowledgeSnippetRepository, InMemoryKnowledgeSnippetRepository>();
        services.AddSingleton<ITextVectorizer, SimpleTextVectorizer>();
        services.AddSingleton<IVectorKnowledgeTool, VectorKnowledgeTool>();
        services.AddSingleton<IStaticResponseTool, StaticResponseTool>();

        return services;
    }
}
