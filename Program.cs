using SolitaAgent.Api.Configuration;

EnvFileLoader.Load(Path.Combine(Directory.GetCurrentDirectory(), ".env"));

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSolitaAgent(builder.Configuration);

var app = builder.Build();

app.UseSolitaAgentMiddleware();
app.Run();
