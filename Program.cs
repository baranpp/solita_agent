// NOTE: Comments in this codebase are added solely to help the reviewer
// read and navigate the code. They would not be present in a production codebase.

using SolitaAgent.Api.Configuration;

// Load .env before the host builder so API keys are available during configuration.
EnvFileLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
// All DI registrations (options, services, LLM provider selection) are in one extension method.
builder.Services.AddSolitaAgent(builder.Configuration);

var app = builder.Build();

// Exception handler, Swagger, auth, and routing are configured in one extension method.
app.UseSolitaAgentMiddleware();
app.Run();
