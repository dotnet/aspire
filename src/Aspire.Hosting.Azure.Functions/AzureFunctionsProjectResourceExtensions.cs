// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Aspire.Hosting.Azure;
using Azure.Provisioning.Storage;

namespace Aspire.Hosting;

/// <summary>
/// Extension methods for <see cref="AzureFunctionsProjectResource"/>.
/// </summary>
public static class AzureFunctionsProjectResourceExtensions
{
    internal const string DefaultAzureFunctionsHostStorageName = "defaultazfuncstorage";

    /// <summary>
    /// Adds an Azure Functions project to the distributed application.
    /// </summary>
    /// <typeparam name="TProject">The type of the project metadata, which must implement <see cref="IProjectMetadata"/> and have a parameterless constructor.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/> to which the Azure Functions project will be added.</param>
    /// <param name="name">The name to be associated with the Azure Functions project. This name will be used for service discovery when referenced in a dependency.</param>
    /// <returns>An <see cref="IResourceBuilder{AzureFunctionsProjectResource}"/> for the added Azure Functions project resource.</returns>
    public static IResourceBuilder<AzureFunctionsProjectResource> AddAzureFunctionsProject<TProject>(this IDistributedApplicationBuilder builder, [ResourceName] string name) where TProject : IProjectMetadata, new()
    {
        var resource = new AzureFunctionsProjectResource(name);

        // Add the default storage resource if it doesn't already exist.
        var storageResourceName = builder.CreateDefaultStorageName();
        AzureStorageResource storage;

        // Azure Functions blob triggers require StorageAccountContributor access to the host storage
        // account when deployed. We assign this role to the host storage resource when running in publish mode.
        if (builder.ExecutionContext.IsPublishMode)
        {
            var configureConstruct = (ResourceModuleConstruct construct) =>
            {
#pragma warning disable AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
                var storageAccount = construct.GetResources().OfType<StorageAccount>().FirstOrDefault(r => r.ResourceName == storageResourceName)
                    ?? throw new InvalidOperationException($"Could not find storage account with '{storageResourceName}' name.");
                construct.Add(storageAccount.CreateRoleAssignment(StorageBuiltInRole.StorageAccountContributor, construct.PrincipalTypeParameter, construct.PrincipalIdParameter));
#pragma warning restore AZPROVISION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
            };
            storage = builder.AddAzureStorage(storageResourceName)
                .ConfigureConstruct(configureConstruct)
                .RunAsEmulator().Resource;
        }
        else
        {
            storage = builder.AddAzureStorage(storageResourceName).RunAsEmulator().Resource;
        }

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
            }

            if (removeStorage)
            {
                data.Model.Resources.Remove(storage);
            }

            return Task.CompletedTask;
        });

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
                // Set ASPNETCORE_URLS to use the non-privileged port 8080 when running in publish mode.
                // We can't use the newer ASPNETCORE_HTTP_PORTS environment variables here since the Azure
                // Functions host is still initialized using the classic WebHostBuilder.
                if (context.ExecutionContext.IsPublishMode)
                {
                    var endpoint = resource.GetEndpoint("http");
                    context.EnvironmentVariables["ASPNETCORE_URLS"] = ReferenceExpression.Create($"http://+:{endpoint.Property(EndpointProperty.TargetPort)}");
                }

                // Set the storage connection string.
                ((IResourceWithAzureFunctionsConfig)resource.HostStorage).ApplyAzureFunctionsConfiguration(context.EnvironmentVariables, "AzureWebJobsStorage");
            })
            .WithArgs(context =>
            {
                // If we're running in publish mode, we don't need to map the port the host should listen on.
                if (builder.ExecutionContext.IsPublishMode)
                {
                    return;
                }
                var http = resource.GetEndpoint("http");
                context.Args.Add("--port");
                context.Args.Add(http.Property(EndpointProperty.TargetPort));
            })
            .WithOtlpExporter()
            .WithFunctionsHttpEndpoint();
    }

    /// <summary>
    /// Configures the Azure Functions project resource to use the specified port as its HTTP endpoint.
    /// This method queries the launch profile of the project to determine the port to
    /// use based on the command line arguments configure in the launch profile,
    /// </summary>
    /// <remarks>
    /// If the Azure Function is running under publish mode, we don't need to map the port
    /// the host should listen on from the launch profile. Instead, we'll use the default
    /// post (8080) used by the .NET container image. The Azure Functions container images
    /// extend the .NET container image and override the default port to 80 for back-compat
    /// purposes. We use the default port (8080) to avoid using privileged ports in the
    /// container image.
    /// </remarks>
    /// <remarks>
    /// /// We provide a custom overload of `WithReference` that allows for the injection of Azure
    /// Functions-specific configuration. The default connection key name that Aspire uses for
    /// resources (ConnectionStrings__{connectionName}) conflicts with Function's expectations
    /// that single-valued config items under the ConnectionStrings prefix must be connection strings.
    /// To work around this, we inject the connection string under the {connectionName} key and
    /// use Aspire's configuration provider model to support the Aspire client integrations.
    /// </remarks>
    /// <param name="builder">The resource builder for the Azure Functions project resource.</param>
    /// <returns>An <see cref="IResourceBuilder{AzureFunctionsProjectResource}"/> for the Azure Functions project resource with the endpoint configured.</returns>
    private static IResourceBuilder<AzureFunctionsProjectResource> WithFunctionsHttpEndpoint(this IResourceBuilder<AzureFunctionsProjectResource> builder)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return builder
                .WithHttpEndpoint(targetPort: 8080)
                .WithHttpsEndpoint(targetPort: 8080);
        }
        var launchProfile = builder.Resource.GetEffectiveLaunchProfile();
        int? port = null;
        if (launchProfile is not null)
        {
            var commandLineArgs = CommandLineArgsParser.Parse(launchProfile.LaunchProfile.CommandLineArgs ?? string.Empty);
            if (commandLineArgs is { Count: > 0 } &&
                commandLineArgs.IndexOf("--port") is var indexOfPort &&
                indexOfPort > -1 &&
                indexOfPort + 1 < commandLineArgs.Count &&
                int.TryParse(commandLineArgs[indexOfPort + 1], CultureInfo.InvariantCulture, out var parsedPort))
            {
                port = parsedPort;
            }
        }
        // When a port is defined in the launch profile, Azure Functions will favor that port over
        // the port configured in the `WithArgs` callback when starting the project. To that end
        // we register an endpoint where the target port matches the port the Azure Functions worker
        // is actually configured to listen on and the endpoint is not proxied by DCP.
        return builder.WithHttpEndpoint(port: port, targetPort: port, isProxied: port == null);
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

    /// <summary>
    /// Injects Azure Functions specific connection information into the environment variables of the Azure Functions
    /// project resource.
    /// </summary>
    /// <typeparam name="TSource">The resource that implements the <see cref="IResourceWithAzureFunctionsConfig"/>.</typeparam>
    /// <param name="destination">The resource where connection information will be injected.</param>
    /// <param name="source">The resource from which to extract the connection string.</param>
    /// <param name="connectionName">An override of the source resource's name for the connection name. The resulting connection name will be connectionName if this is not null.</param>
    public static IResourceBuilder<AzureFunctionsProjectResource> WithReference<TSource>(this IResourceBuilder<AzureFunctionsProjectResource> destination, IResourceBuilder<TSource> source, string? connectionName = null)
        where TSource : IResourceWithConnectionString, IResourceWithAzureFunctionsConfig
    {
        return destination.WithEnvironment(context =>
        {
            connectionName ??= source.Resource.Name;
            source.Resource.ApplyAzureFunctionsConfiguration(context.EnvironmentVariables, connectionName);
        });
    }

    private static string CreateDefaultStorageName(this IDistributedApplicationBuilder builder)
    {
        var applicationHash = builder.Configuration["AppHost:Sha256"]![..5].ToLowerInvariant();
        return $"{DefaultAzureFunctionsHostStorageName}{applicationHash}";
    }
}
