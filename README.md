# Solita Agent Backend

Small ASP.NET Core backend that exposes an LLM-backed agent through a single REST endpoint. The agent uses two tools and supports switching between Gemini and Groq as the LLM provider via an environment variable.

This project follows **Clean Architecture** and **Clean Code** principles (Uncle Bob), within the time allowed for this task. Dependencies point inward, all service contracts are defined as interfaces in Core, and Infrastructure implements them etc.

## Shortcuts Taken

- No automated tests in this iteration.
- No persistent storage, authentication, chat memory, or external vector DB.
- Swagger is enabled for all environments because this is a take-home demo project, not a production deployment.
- The `.env` loader is intentionally tiny and only supports simple `KEY=value` entries.

## Some of the Things I Would Improve Next

- Add tests.
- Add richer local knowledge and better text preprocessing.
- Add structured logging for tool decisions and fallback reasons.
- If the local vector DB were replaced with an external store, I would add failure modes for connection errors, timeouts, and similar infrastructure failures — the same way the LLM provider layer already handles unavailability.
- Add session management (conversation history, user context) for a production environment.
- In a production environment I would introduce mapping at every layer boundary (and use a mapper library if needed) for true separation of concerns with high cohesion and low coupling.
- Be more intentional with custom objects in method parameters and return types rather than passing primitives through layers.
- Be more intentional with variable naming and file naming as the project scales.
- Look into adding validator in the service layer to validate potential business rules.
- More intentional custom exception classes.

## Design Notes

- The project follows clean architecture: Api, Core, and Infrastructure are separated with clear dependency boundaries.
- Core contains business logic and interfaces with zero external dependencies.
- Infrastructure implements Core's interfaces for LLM providers, repositories, and tools.
- The controller is intentionally thin and delegates the workflow to `AgentOrchestrator`.
- The service layer depends on provider-neutral interfaces (`IToolSelectionClient`, `IAnswerGenerationClient`), with Gemini and Groq implementations that can be swapped via the `LLM_PROVIDER` environment variable.
- The vector search is local and simple by design: lowercase normalization, punctuation stripping, term-frequency vectors, and cosine similarity.
- The API returns the selected tool and whether a fallback happened so the tool flow is easy to demo.
- A small startup loader reads `.env` into process environment variables, while real OS environment variables still win if both are set.

## Tools

- `search_vector_knowledge` searches a local in-memory vector store built from hardcoded text snippets.
- `get_predefined_response` returns a fixed fallback response.

## Agent Flow

1. The LLM selects which tool to call based on the user's question (tool selection).
2. The selected tool executes and returns its output.
3. The LLM synthesizes a natural language answer from the tool output (answer generation, RAG pattern).

## Project Structure

```
Program.cs                          Entry point and DI composition root
Api/
  Configuration/                    Startup utilities (EnvFileLoader)
  Controllers/                      REST endpoint
  Exceptions/                       API-layer exceptions
  Validators/                       Input sanitization
Core/
  Contracts/                        Shared DTOs and constants
  Exceptions/                       Domain exceptions
  Models/                           Domain models
  Options/                          Domain configuration
  Prompts/                          Shared LLM prompt constants
  Services/                         Business logic, interfaces
Infrastructure/
  LlmProviders/
    Gemini/                         Gemini implementation
    Groq/                           Groq implementation + Models/
  Repositories/                     Data access
  Tools/                            Tool implementations
```

## Run

1. Create a `.env` file with your API key and preferred provider:

```text
GROQ_API_KEY="your-groq-api-key"
LLM_PROVIDER=groq
```

Or to use Gemini:

```text
GEMINI_API_KEY="your-gemini-api-key"
LLM_PROVIDER=gemini
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

Response:

```json
{
  "question": "Is it warmer in France than Sweden?",
  "selectedTool": "search_vector_knowledge",
  "answer": "Yes, it is warmer in France than Sweden. This is due to France's more southerly location and its proximity to the Gulf Stream.",
  "fallbackUsed": false,
  "llmUnavailable": false
}
```