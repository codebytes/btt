var builder = WebApplication.CreateBuilder(args);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

var cacheClient = builder.AddRedisClientBuilder("cache");
cacheClient.WithOutputCache();

// In Azure the cache is provisioned as managed Redis with Entra ID auth (no password).
// Locally it runs as a container using a password-based connection string.
if (!builder.Environment.IsDevelopment())
{
    cacheClient.WithAzureAuthentication();
}

// Add services to the container.
builder.Services.AddProblemDetails();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseOutputCache();

app.MapDefaultEndpoints();

app.UseFileServer();

app.Run();
