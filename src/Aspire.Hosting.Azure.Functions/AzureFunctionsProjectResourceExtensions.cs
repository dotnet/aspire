// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Extension methods for <see cref="AzureFunctionsProjectResource"/>.
/// </summary>
public static class AzureFunctionsProjectResourceExtensions
{
    /// <summary>
    /// Adds an Azure Functions project to the distributed application.
    /// </summary>
    /// <typeparam name="TProject">The type of the project metadata, which must implement <see cref="IProjectMetadata"/> and have a parameterless constructor.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to which the Azure Functions project will be added.</param>
    /// <param name="name">The name to be associated with the Azure Functions project. This name will be used for service discovery when referenced in a dependency.</param>
    /// <returns>An <see cref="IResourceBuilder{AzureFunctionsProjectResource}"/> for the added Azure Functions project resource.</returns>
    public static IResourceBuilder<AzureFunctionsProjectResource> AddAzureFunctionsProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IProjectMetadata, new()
    {
        var resource = new AzureFunctionsProjectResource(name);

        // Add the default storage resource if it doesn't already exist.
        var storage = builder.Resources.OfType<AzureStorageResource>().FirstOrDefault(r => r.Name == "azure-functions-default-storage");

        if (storage is null)
        {
            storage = builder.AddAzureStorage("azure-functions-default-storage").RunAsEmulator().Resource;

            builder.Eventing.Subscribe<BeforeStartEvent>((data, token) =>
            {
                var removeStorage = true;
                // Look at all of the resources and if none of them use the default storage, then we can remove it.
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
            .WithAnnotation(new TProject())
            .WithEnvironment(context =>
            {
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES"] = "true";
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES"] = "true";
                context.EnvironmentVariables["OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY"] = "in_memory";
                context.EnvironmentVariables["ASPNETCORE_FORWARDEDHEADERS_ENABLED"] = "true";
                context.EnvironmentVariables["FUNCTIONS_WORKER_RUNTIME"] = "dotnet-isolated";

                // Set the storage connection string.
                ((IResourceWithAzureFunctionsConfig)resource.HostStorage).ApplyAzureFunctionsConfiguration(context.EnvironmentVariables, "Storage");
            })
            .WithArgs(context =>
            {
                var http = resource.GetEndpoint("http");
                context.Args.Add("--port");
                context.Args.Add(http.Property(EndpointProperty.TargetPort));
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
    /// Configures the Azure Functions project resource to use the specified Azure Storage resource as its host storage.
    /// </summary>
    /// <param name="builder">The resource builder for the Azure Functions project resource.</param>
    /// <param name="storage">The resource builder for the Azure Storage resource to be used as host storage.</param>
    /// <returns>The resource builder for the Azure Functions project resource, configured with the specified host storage.</returns>
    public static IResourceBuilder<AzureFunctionsProjectResource> WithHostStorage(this IResourceBuilder<AzureFunctionsProjectResource> builder, IResourceBuilder<AzureStorageResource> storage)
    {
        builder.Resource.HostStorage = storage.Resource;
        return builder;
    }
}
