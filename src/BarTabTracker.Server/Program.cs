using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BarTabTracker.Server.Api;
using BarTabTracker.Server.Auth;
using BarTabTracker.Server.Location;
using BarTabTracker.Server.Splitting;
using BarTabTracker.Server.Storage;

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
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)));
builder.Services.AddProblemDetails();
builder.Services.AddBarTabStorage();
builder.Services.AddScoped<SplitCalculator>();
builder.Services.AddHttpClient<IReverseGeocoder, NominatimReverseGeocoder>(client =>
{
    client.BaseAddress = new Uri("https://nominatim.openstreetmap.org/");
    client.DefaultRequestHeaders.UserAgent.ParseAdd("BarTabTracker/1.0 (https://github.com/codebytes/btt)");
});
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("geocode", httpContext =>
    {
        var subject = httpContext.User.FindFirstValue(BarTabClaimTypes.OAuthSubject)
            ?? httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var remoteIp = httpContext.Connection.RemoteIpAddress?.ToString();
        var partitionKey = !string.IsNullOrWhiteSpace(subject)
            ? $"user:{subject}"
            : $"ip:{remoteIp ?? "unknown"}";

        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        });
    });
});
var microsoftAuthEnabled = builder.AddBarTabAuthentication();

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
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.Logger.LogInformation(microsoftAuthEnabled
    ? "Microsoft Entra ID authentication is enabled."
    : "Microsoft Entra ID credentials are not configured; Development can use /api/auth/dev-login.");

app.MapDefaultEndpoints();
app.MapBarTabApi(microsoftAuthEnabled);

app.UseFileServer();

app.Run();
