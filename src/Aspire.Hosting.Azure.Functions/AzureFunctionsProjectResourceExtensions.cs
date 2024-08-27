using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Extension methods for <see cref="AzureFunctionsProjectResource"/>.
/// </summary>
public static class AzureFunctionsProjectResourceExtensions
{
    /// <summary>
    /// Add an Azure Functions project to the distributed application.
    /// </summary>
    /// <typeparam name="TProject"></typeparam>
    /// <param name="builder"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static IResourceBuilder<AzureFunctionsProjectResource> AddAzureFunctionsProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IProjectMetadata, new()
    {
        var projectDirectory = Path.GetDirectoryName(new TProject().ProjectPath)!;
        var resource = new AzureFunctionsProjectResource(name, "func", projectDirectory);
        return builder.AddResource(resource)
            .WithArgs(context =>
            {
                var http = resource.GetEndpoint("http");

                context.Args.Add("host");
                context.Args.Add("start");
                context.Args.Add("--verbose");
                context.Args.Add("--csharp");
                context.Args.Add("--port");
                context.Args.Add(http.Property(EndpointProperty.TargetPort));
                context.Args.Add("--language-worker");
                context.Args.Add("dotnet-isolated");
            })
            .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true")
            .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true")
            .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory")
            .WithEnvironment("ASPNETCORE_FORWARDEDHEADERS_ENABLED", "true")
            .WithEnvironment("FUNCTIONS_WORKER_RUNTIME", "dotnet-isolated")
            .WithOtlpExporter()
            .WithHttpEndpoint()
            .WithManifestPublishingCallback(async (context) =>
            {
                context.Writer.WriteString("type", "function.v0");
                context.Writer.WriteString("path", context.GetManifestRelativePath(new TProject().ProjectPath));
                await context.WriteEnvironmentVariablesAsync(resource).ConfigureAwait(false);
                context.Writer.WriteStartObject("bindings");
                foreach (var s in new string[] { "http", "https" })
                {
                    context.Writer.WriteStartObject(s);
                    context.Writer.WriteString("scheme", s);
                    context.Writer.WriteString("protocol", "tcp");
                    context.Writer.WriteString("transport", "http");
                    context.Writer.WriteBoolean("external", true);
                    context.Writer.WriteEndObject();
                }
                context.Writer.WriteEndObject();
            });
    }

    /// <remarks>
    /// This implementation demonstrates the approach of "teaching Aspire about Functions" where we take advantage of
    /// Aspire's ConnectionStringExpression to map to the connection strings format that Azure Functions understands.
    /// An alternative approach here is to "teach Functions about Aspire" where integrate the Aspire configuration
    /// model into the Azure Functions worker extensions.
    /// </remarks>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static IResourceBuilder<AzureFunctionsProjectResource> WithReference(this IResourceBuilder<AzureFunctionsProjectResource> builder, IResourceBuilder<AzureQueueStorageResource> source, string? connectionName = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        return builder.WithEnvironment(context =>
        {
            var prefix = connectionName ?? "Storage";
            if (source.Resource.Parent.IsEmulator)
            {
                context.EnvironmentVariables[prefix] = source.Resource.Parent.GetEmulatorConnectionString();
            }
            else
            {
                context.EnvironmentVariables[$"{prefix}__queueServiceUri"] = source.Resource.ConnectionStringExpression;
            }
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="source"></param>
    /// <param name="connectionName"></param>
    /// <returns></returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static IResourceBuilder<AzureFunctionsProjectResource> WithReference(this IResourceBuilder<AzureFunctionsProjectResource> builder, IResourceBuilder<AzureBlobStorageResource> source, string? connectionName = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        return builder.WithEnvironment(context =>
        {
            var prefix = connectionName ?? "Storage";
            if (source.Resource.Parent.IsEmulator)
            {
                context.EnvironmentVariables[prefix] = source.Resource.Parent.GetEmulatorConnectionString();
            }
            else
            {
                context.EnvironmentVariables[$"{prefix}__blobServiceUri"] = source.Resource.ConnectionStringExpression;
            }
        });
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="source"></param>
    /// <param name="connectionName"></param>
    /// <returns></returns>
#pragma warning disable RS0026 // Do not add multiple public overloads with optional parameters
    public static IResourceBuilder<AzureFunctionsProjectResource> WithReference(this IResourceBuilder<AzureFunctionsProjectResource> builder, IResourceBuilder<AzureEventHubsResource> source, string? connectionName = null)
#pragma warning restore RS0026 // Do not add multiple public overloads with optional parameters
    {
        return builder.WithEnvironment(context =>
        {
            connectionName ??= source.Resource.Name;
            if (source.Resource.IsEmulator)
            {
                context.EnvironmentVariables[connectionName] = source.Resource.ConnectionStringExpression;
            }
            else
            {
                context.EnvironmentVariables[$"{connectionName}__fullyQualifiedNamespace"] = source.Resource.ConnectionStringExpression;
            }
        });
    }
}
