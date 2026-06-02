var builder = DistributedApplication.CreateBuilder(args);

builder.AddAzureContainerAppEnvironment("aca");

var cache = builder.AddAzureManagedRedis("cache")
    .RunAsContainer();

var server = builder.AddProject<Projects.BarTabTracker_Server>("server")
    .WithReference(cache)
    .WaitFor(cache)
    .WithHttpHealthCheck("/health")
    .WithExternalHttpEndpoints();

var webfrontend = builder.AddViteApp("webfrontend", "../frontend")
    .WithReference(server)
    .WaitFor(server);

server.PublishWithContainerFiles(webfrontend, "wwwroot");

builder.Build().Run();
