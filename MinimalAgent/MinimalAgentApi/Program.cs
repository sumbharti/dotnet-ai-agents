using MinimalAgentApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapGroup("/api")
    .RandomApiEndpoints()
    .WithTags(nameof(RandomApi));

app.Run();
