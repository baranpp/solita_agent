# Solita Agent Backend

Small ASP.NET Core backend that exposes a Gemini-backed agent through a single REST endpoint. The agent can use exactly two tools:

- `search_vector_knowledge` searches a local in-memory vector store built from hardcoded text snippets.
- `get_predefined_response` returns a fixed fallback response.

## Requirements Covered

- C# ASP.NET Core backend
- Gemini `gemini-2.5-flash` as the agent model
- Function-calling mode `ANY` so the model must choose a tool
- Mocked/local vector DB only
- Single endpoint plus Swagger
- Clear `503` response when `GEMINI_API_KEY` is missing

## Run

1. Create a local `.env` file and add your Gemini API key:

```text
GEMINI_API_KEY="your-api-key"
```

2. Start the API:

```bash
dotnet run
```

3. Open Swagger at the URL printed by ASP.NET Core, for example:

```text
https://localhost:xxxx/swagger
```

## API

`GET /api/agent/ask?question=...`

Example:

```bash
curl "http://localhost:5242/api/agent/ask?question=Is%20it%20warmer%20in%20France%20than%20Sweden%3F"
```

Successful responses look like:

```json
{
  "question": "Is it warmer in France than Sweden?",
  "selectedTool": "search_vector_knowledge",
  "answer": "It is warmer in France than Sweden.",
  "fallbackUsed": false
}
```

## Design Notes

- The controller is intentionally thin and delegates the workflow to `AgentOrchestrator`.
- Gemini is wrapped behind `IGeminiAgentClient` so SDK details stay isolated.
- The repository pattern is used only for the local knowledge store because that is the only place where persistence-style access adds value in this scope.
- The vector search is local and simple by design: lowercase normalization, punctuation stripping, term-frequency vectors, and cosine similarity.
- The API returns the selected tool and whether a fallback happened so the tool flow is easy to demo.
- A small startup loader reads `.env` into process environment variables, while real OS environment variables still win if both are set.

## Shortcuts Taken

- No automated tests in this iteration.
- No second Gemini pass after tool execution; the selected tool output is returned directly.
- No persistent storage, authentication, chat memory, or external vector DB.
- Swagger is enabled for all environments because this is a take-home demo project, not a production deployment.
- The `.env` loader is intentionally tiny and only supports simple `KEY=value` entries.

## What I Would Improve Next

- Add automated tests for the orchestrator and vector search behavior.
- Add richer local knowledge and better text preprocessing.
- Add structured logging for tool decisions and fallback reasons.
