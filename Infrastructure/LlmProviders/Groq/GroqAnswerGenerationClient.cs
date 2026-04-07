using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using SolitaAgent.Core.Services;
using SolitaAgent.Infrastructure.LlmProviders.Groq.Exceptions;
using SolitaAgent.Infrastructure.LlmProviders.Groq.Models;

namespace SolitaAgent.Infrastructure.LlmProviders.Groq;

public sealed class GroqAnswerGenerationClient : IAnswerGenerationClient
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

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GroqOptions _options;

    public GroqAnswerGenerationClient(
        IHttpClientFactory httpClientFactory,
        IOptions<GroqOptions> options)
    {
        _httpClientFactory = httpClientFactory;
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
            throw new MissingGroqApiKeyException();
        }

        var prompt = BuildPrompt(question, toolName, toolOutput, similarityScore);

        var request = new GroqChatRequest
        {
            Model = _options.Model,
            Messages =
            [
                new GroqChatMessage { Role = "system", Content = SystemInstruction },
                new GroqChatMessage { Role = "user", Content = prompt }
            ],
            Temperature = 0
        };

        var response = await SendRequestAsync(request, cancellationToken);

        return response.Choices.FirstOrDefault()?.Message.Content ?? toolOutput;
    }

    private async Task<GroqChatResponse> SendRequestAsync(
        GroqChatRequest request,
        CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("Groq");

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        httpRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer", _options.ApiKey);
        httpRequest.Content = JsonContent.Create(request);

        using var httpResponse = await client.SendAsync(httpRequest, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            var errorBody = await httpResponse.Content.ReadFromJsonAsync<GroqErrorResponse>(
                cancellationToken: cancellationToken);
            var message = errorBody?.Error?.Message ?? "Groq API request failed.";
            throw new GroqApiException(httpResponse.StatusCode, message);
        }

        return await httpResponse.Content.ReadFromJsonAsync<GroqChatResponse>(
            cancellationToken: cancellationToken)
            ?? throw new GroqApiException(System.Net.HttpStatusCode.InternalServerError, "Groq returned an empty response.");
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
