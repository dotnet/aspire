#:sdk Aspire.AppHost.Sdk@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.NodeJs@!!REPLACE_WITH_LATEST_VERSION!!
#:package Aspire.Hosting.Python@!!REPLACE_WITH_LATEST_VERSION!!
#if UseRedisCache
#:package Aspire.Hosting.Redis@!!REPLACE_WITH_LATEST_VERSION!!
#endif
#:package CommunityToolkit.Aspire.Hosting.NodeJS.Extensions@9.8.0

#pragma warning disable ASPIREHOSTINGPYTHON001

var builder = DistributedApplication.CreateBuilder(args);

#if UseRedisCache
var cache = builder.AddRedis("cache");

#endif
var app = builder.AddUvicornApp("app", "./app", "app:app")
    .WithUvEnvironment()
    .WithExternalHttpEndpoints()
#if UseRedisCache
    .WithReference(cache)
    .WaitFor(cache)
#endif
    .WithHttpHealthCheck("/health");

var frontend = builder.AddViteApp("frontend", "./frontend")
    .WithNpmPackageInstallation()
    .WithReference(app)
    .WaitFor(app)
    .PublishAsDockerFile(c =>
    {
#pragma warning disable ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        c.WithDockerfileBuilder("./frontend", async context =>
        {
            context.Builder.From("node:22-slim")
                .Copy(".", "/app")
                .WorkDir("/app")
                .Run("npm install")
                .Run("npm run build");
        });
#pragma warning restore ASPIREDOCKERFILEBUILDER001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    })
    .WithAnnotation(new StaticDockerFilesAnnotation() { SourcePath = "/app/dist" });

app.PublishWithStaticFiles(frontend, "./static");

builder.Build().Run();
