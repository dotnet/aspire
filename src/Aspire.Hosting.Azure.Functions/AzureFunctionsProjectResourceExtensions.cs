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

        // Add the default storage resource if it doesn't already exist.
        var storage = builder.Resources.OfType<AzureStorageResource>().FirstOrDefault(r => r.Name == "azure-functions-default-storage");

        if (storage is null)
        {
            storage = builder.AddAzureStorage("azure-functions-default-storage").RunAsEmulator().Resource;

            builder.Eventing.Subscribe<BeforeStartEvent>((data, token) =>
            {
                var removeStorage = true;
                // Look at all of the resources and if non of them use the default storage, then we can remove it.
                // This is because we're unable to cleanly add a resource to the builder from within a callback.
                foreach (var item in data.Model.Resources.OfType<AzureFunctionsProjectResource>())
                {
                    if (item.HostStorage == storage)
                    {
                        removeStorage = false;
                    }

                    // Before the resource starts, we want to apply the azure functions specific environment variables.
                    // we look at all of the environment variables and apply the configuration for any resources that implement IResourceWithAzureFunctionsConfig.
                    item.Annotations.Add(new EnvironmentCallbackAnnotation(static context =>
                    {
                        var functionsConfigMapping = new Dictionary<string, IResourceWithAzureFunctionsConfig>();
                        var valuesToRemove = new List<string>();

                        foreach (var (envName, val) in context.EnvironmentVariables)
                        {
                            var (name, config) = val switch
                            {
                                IResourceWithAzureFunctionsConfig c => (c.Name, c),
                                ConnectionStringReference conn when conn.Resource is IResourceWithAzureFunctionsConfig c => (conn.ConnectionName ?? c.Name, c),
                                _ => ("", null)
                            };

                            if (config is not null)
                            {
                                valuesToRemove.Add(envName);
                                functionsConfigMapping[name] = config;
                            }
                        }

                        // REVIEW: We need to remove the existing values before adding the new ones as there's a conflict with the connection strings.
                        // we don't want to do this because it'll stop the aspire components from working in functions projects.
                        foreach (var envName in valuesToRemove)
                        {
                            context.EnvironmentVariables.Remove(envName);
                        }

                        foreach (var (name, config) in functionsConfigMapping)
                        {
                            config.ApplyAzureFunctionsConfiguration(context.EnvironmentVariables, name);
                        }

                        return Task.CompletedTask;
                    }));
                }

                if (removeStorage)
                {
                    data.Model.Resources.Remove(storage);
                }

                return Task.CompletedTask;
            });
        }

        resource.HostStorage = storage;

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
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES"] = "true";
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES"] = "true";
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY"] = "in_memory";
                context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                context.EnvironmentVariables["FUNCTIONS_WORKER_RUNTIME"] = "dotnet-isolated";

                if (resource.HostStorage.IsEmulator)
                {
                    context.EnvironmentVariables["Storage"] = resource.HostStorage.GetEmulatorConnectionString();
                }
                else
                {
                    context.EnvironmentVariables["Storage__blobServiceUri"] = resource.HostStorage.BlobEndpoint;
                    context.EnvironmentVariables["Storage__queueServiceUri"] = resource.HostStorage.QueueEndpoint;
                }
            })
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

    /// <summary>
    /// 
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="storage"></param>
    /// <returns></returns>
    public static IResourceBuilder<AzureFunctionsProjectResource> WithHostStorage(this IResourceBuilder<AzureFunctionsProjectResource> builder, IResourceBuilder<AzureStorageResource> storage)
    {
        builder.Resource.HostStorage = storage.Resource;
        return builder;
    }
}
