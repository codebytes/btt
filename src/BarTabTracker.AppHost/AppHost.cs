var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca");

var cache = builder.AddAzureManagedRedis("cache")
    .RunAsContainer();

// Microsoft Entra ID credentials, modeled as Aspire parameters so they persist
// across deploys (the AppHost is the source of truth for the Container App definition).
// TenantId and ClientId are not secret and carry default values so deploys stay
// non-interactive. The client secret is a secret parameter — it flows into Azure as a
// Container App secret / Key Vault reference and is NEVER stored in source.
var entraTenantId = builder.AddParameter("entra-tenant-id", "de44e9b8-5a34-4f88-9b3d-b6e191f749c3", publishValueAsDefault: true);
var entraClientId = builder.AddParameter("entra-client-id", "5ac10106-465d-4c16-bbc0-78d348666482", publishValueAsDefault: true);
var entraClientSecret = builder.AddParameter("entra-client-secret", secret: true);

var server = builder.AddProject<Projects.BarTabTracker_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithEnvironment("Authentication__Microsoft__TenantId", entraTenantId)
    .WithEnvironment("Authentication__Microsoft__ClientId", entraClientId)
    .WithEnvironment("Authentication__Microsoft__ClientSecret", entraClientSecret)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
