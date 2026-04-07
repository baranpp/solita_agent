using Google.GenAI;
using Google.GenAI.Types;
using Microsoft.Extensions.Options;
using SolitaAgent.Core.Services;
using SolitaAgent.Infrastructure.LlmProviders.Gemini.Exceptions;

namespace SolitaAgent.Infrastructure.LlmProviders.Gemini;

public sealed class GeminiAnswerGenerationClient : IAnswerGenerationClient
{
    private const string SystemInstruction = """
        You are a helpful assistant that answers user questions based on tool results
        from a local knowledge base.

        You will receive:
        - The user's original question.
        - The name of the tool that was used.
        - The output of that tool.
        - Optionally, a similarity score (0 to 1) indicating how well the retrieved
          snippet matched the question.

        Rules:
        - If a knowledge base snippet was retrieved with a reasonable similarity score,
          use it to answer the question naturally and conversationally.
        - If the similarity score is low or the snippet does not clearly answer the
          question, acknowledge what you found but be honest that the information may
          not fully address their question.
        - If the fallback tool was used, politely tell the user that their question
          could not be answered from the available knowledge base.
        - Keep answers concise — one to three sentences.
        - Do not invent facts beyond what the tool output provides.
        """;

    private readonly GeminiOptions _options;

    public GeminiAnswerGenerationClient(IOptions<GeminiOptions> options)
    {
        _options = options.Value;
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        string toolName,
        string toolOutput,
        double? similarityScore,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new MissingGeminiApiKeyException();
        }

        var prompt = BuildPrompt(question, toolName, toolOutput, similarityScore);

        var client = new Client(apiKey: _options.ApiKey, vertexAI: false);
        var response = await client.Models.GenerateContentAsync(
            model: _options.Model,
            contents: prompt,
            config: new GenerateContentConfig
            {
                SystemInstruction = new Content
                {
                    Parts = [new Part { Text = SystemInstruction }]
                },
                Temperature = 0
            },
            cancellationToken: cancellationToken);

        return response.Text ?? toolOutput;
    }

    private static string BuildPrompt(
        string question,
        string toolName,
        string toolOutput,
        double? similarityScore)
    {
        var scoreInfo = similarityScore.HasValue
            ? $"\nSimilarity score: {similarityScore.Value:F2}"
            : string.Empty;

        return $"""
            User question: {question}
            Tool used: {toolName}
            Tool output: {toolOutput}{scoreInfo}
            """;
    }
}
