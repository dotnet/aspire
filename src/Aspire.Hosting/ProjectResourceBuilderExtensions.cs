// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Dashboard;
using Aspire.Hosting.Utils;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for <see cref="IDistributedApplicationBuilder"/> to add and configure project resources.
/// </summary>
public static class ProjectResourceBuilderExtensions
{
    private const string AspNetCoreForwardedHeadersEnabledVariableName = "ASPNETCORE_FORWARDEDHEADERS_ENABLED";

    /// <summary>
    /// Adds a .NET project to the application model.
    /// </summary>
    /// <typeparam name="TProject">A type that represents the project reference.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload of the <see cref="AddProject{TProject}(IDistributedApplicationBuilder, string)"/> method takes
    /// a <typeparamref name="TProject"/> type parameter. The <typeparamref name="TProject"/> type parameter is constrained
    /// to types that implement the <see cref="IProjectMetadata"/> interface.
    /// </para>
    /// <para>
    /// Classes that implement the <see cref="IProjectMetadata"/> interface are generated when a .NET project is added as a reference
    /// to the app host project. The generated class contains a property that returns the path to the referenced project file. Using this path
    /// .NET Aspire parses the <c>launchSettings.json</c> file to determine which launch profile to use when running the project, and
    /// what endpoint configuration to automatically generate.
    /// </para>
    /// <para>
    /// The name of the automatically generated project metadata type is a normalized version of the project name. Periods, dashes, and
    /// spaces in project names are converted to underscores. This normalization may lead to naming conflicts. If a conflict occurs the <c>&lt;ProjectReference /&gt;</c>
    /// that references the project can have a <c>AspireProjectMetadataTypeName="..."</c> attribute added to override the name.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example of adding a project to the application model.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// builder.AddProject&lt;Projects.InventoryService&gt;("inventoryservice");
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name) where TProject : IProjectMetadata, new()
    {
        var project = new ProjectResource(name);
        return builder.AddResource(project)
                      .WithAnnotation(new TProject())
                      .WithProjectDefaults(excludeLaunchProfile: false, launchProfileName: null);
    }

    /// <summary>
    /// Adds a .NET project to the application model. 
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <param name="projectPath">The path to the project file.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload of the <see cref="AddProject(IDistributedApplicationBuilder, string, string)"/> method adds a project to the application
    /// model using an path to the project file. This allows for projects to be referenced that may not be part of the same solution. If the project
    /// path is not an absolute path then it will be computed relative to the app host directory.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a project to the app model via a project path.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// builder.AddProject("inventoryservice", @"..\InventoryService\InventoryService.csproj");
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, string name, string projectPath)
    {
        var project = new ProjectResource(name);

        projectPath = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectPath));

        return builder.AddResource(project)
                      .WithAnnotation(new ProjectMetadata(projectPath))
                      .WithProjectDefaults(excludeLaunchProfile: false, launchProfileName: null);
    }

    /// <summary>
    /// Adds a .NET project to the application model. By default, this will exist in a Projects namespace. e.g. Projects.MyProject.
    /// If the project is not in a Projects namespace, make sure a project reference is added from the AppHost project to the target project.
    /// </summary>
    /// <typeparam name="TProject">A type that represents the project reference.</typeparam>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <param name="launchProfileName">The launch profile to use. If <c>null</c> then no launch profile will be used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload of the <see cref="AddProject{TProject}(IDistributedApplicationBuilder, string)"/> method takes
    /// a <typeparamref name="TProject"/> type parameter. The <typeparamref name="TProject"/> type parameter is constrained
    /// to types that implement the <see cref="IProjectMetadata"/> interface.
    /// </para>
    /// <para>
    /// Classes that implement the <see cref="IProjectMetadata"/> interface are generated when a .NET project is added as a reference
    /// to the app host project. The generated class contains a property that returns the path to the referenced project file. Using this path
    /// .NET Aspire parses the <c>launchSettings.json</c> file to determine which launch profile to use when running the project, and
    /// what endpoint configuration to automatically generate.
    /// </para>
    /// <para>
    /// The name of the automatically generated project metadata type is a normalized version of the project name. Periods, dashes, and
    /// spaces in project names are converted to underscores. This normalization may lead to naming conflicts. If a conflict occurs the <c>&lt;ProjectReference /&gt;</c>
    /// that references the project can have a <c>AspireProjectMetadataTypeName="..."</c> attribute added to override the name.
    /// </para>
    /// </remarks>
    /// <example>
    /// Example of adding a project to the application model.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// builder.AddProject&lt;Projects.InventoryService&gt;("inventoryservice", launchProfileName: "otherLaunchProfile");
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ProjectResource> AddProject<TProject>(this IDistributedApplicationBuilder builder, string name, string? launchProfileName) where TProject : IProjectMetadata, new()
    {
        var project = new ProjectResource(name);
        return builder.AddResource(project)
                      .WithAnnotation(new TProject())
                      .WithProjectDefaults(excludeLaunchProfile: launchProfileName is null, launchProfileName);
    }

    /// <summary>
    /// Adds a .NET project to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used for service discovery when referenced in a dependency.</param>
    /// <param name="projectPath">The path to the project file.</param>
    /// <param name="launchProfileName">The launch profile to use. If <c>null</c> then no launch profile will be used.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This overload of the <see cref="AddProject(IDistributedApplicationBuilder, string, string)"/> method adds a project to the application
    /// model using an path to the project file. This allows for projects to be referenced that may not be part of the same solution. If the project
    /// path is not an absolute path then it will be computed relative to the app host directory.
    /// </para>
    /// </remarks>
    /// <example>
    /// Add a project to the app model via a project path.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    /// 
    /// builder.AddProject("inventoryservice", @"..\InventoryService\InventoryService.csproj", launchProfileName: "otherLaunchProfile");
    /// 
    /// builder.Build().Run();
    /// </code>
    /// </example>
    public static IResourceBuilder<ProjectResource> AddProject(this IDistributedApplicationBuilder builder, string name, string projectPath, string? launchProfileName)
    {
        var project = new ProjectResource(name);

        projectPath = PathNormalizer.NormalizePathForCurrentPlatform(Path.Combine(builder.AppHostDirectory, projectPath));

        return builder.AddResource(project)
                      .WithAnnotation(new ProjectMetadata(projectPath))
                      .WithProjectDefaults(excludeLaunchProfile: launchProfileName is null, launchProfileName);
    }

    private static IResourceBuilder<ProjectResource> WithProjectDefaults(this IResourceBuilder<ProjectResource> builder, bool excludeLaunchProfile, string? launchProfileName)
    {
        // We only want to turn these on for .NET projects, ConfigureOtlpEnvironment works for any resource type that
        // implements IDistributedApplicationResourceWithEnvironment.
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EXCEPTION_LOG_ATTRIBUTES", "true");
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_EMIT_EVENT_LOG_ATTRIBUTES", "true");
        // .NET SDK has experimental support for retries. Enable with env var.
        // https://github.com/open-telemetry/opentelemetry-dotnet/pull/5495
        // Remove once retry feature in opentelemetry-dotnet is enabled by default.
        builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_OTLP_RETRY", "in_memory");

        // OTEL settings that are used to improve local development experience.
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            // Disable URL query redaction, e.g. ?myvalue=Redacted
            builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_ASPNETCORE_DISABLE_URL_QUERY_REDACTION", "true");
            builder.WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_HTTPCLIENT_DISABLE_URL_QUERY_REDACTION", "true");
        }

        builder.WithOtlpExporter();
        builder.ConfigureConsoleLogs();

        var projectResource = builder.Resource;

        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            builder.WithEnvironment(context =>
            {
                // If we have any endpoints & the forwarded headers wasn't disabled then add it
                if (projectResource.GetEndpoints().Any() && !projectResource.Annotations.OfType<DisableForwardedHeadersAnnotation>().Any())
                {
                    context.EnvironmentVariables[AspNetCoreForwardedHeadersEnabledVariableName] = "true";
                }
            });
        }

        if (excludeLaunchProfile)
        {
            builder.WithAnnotation(new ExcludeLaunchProfileAnnotation());
        }
        else if (!string.IsNullOrEmpty(launchProfileName))
        {
            builder.WithAnnotation(new LaunchProfileAnnotation(launchProfileName));
        }
        else
        {
            var appHostDefaultLaunchProfileName = builder.ApplicationBuilder.Configuration["AppHost:DefaultLaunchProfileName"]
                ?? Environment.GetEnvironmentVariable("DOTNET_LAUNCH_PROFILE");
            if (!string.IsNullOrEmpty(appHostDefaultLaunchProfileName))
            {
                builder.WithAnnotation(new DefaultLaunchProfileAnnotation(appHostDefaultLaunchProfileName));
            }
        }

        var effectiveLaunchProfile = excludeLaunchProfile ? null : projectResource.GetEffectiveLaunchProfile(throwIfNotFound: true);
        var launchProfile = effectiveLaunchProfile?.LaunchProfile;

        // Process the launch profile and turn it into environment variables and endpoints.
        var config = GetConfiguration(projectResource);
        var kestrelEndpoints = config.GetSection("Kestrel:Endpoints").GetChildren();

        // Get all the Kestrel configuration endpoint bindings, grouped by scheme
        var kestrelEndpointsByScheme = kestrelEndpoints
            .Where(endpoint => endpoint["Url"] is string)
            .Select(endpoint => new
            {
                EndpointName = endpoint.Key,
                BindingAddress = BindingAddress.Parse(endpoint["Url"]!)
            })
            .GroupBy(entry => entry.BindingAddress.Scheme);

        // Helper to change the transport to http2 if needed
        var isHttp2ConfiguredInAppSettings = config["Kestrel:EndpointDefaults:Protocols"] == "Http2";
        var adjustTransport = (EndpointAnnotation e) => e.Transport = isHttp2ConfiguredInAppSettings ? "http2" : e.Transport;

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            foreach (var schemeGroup in kestrelEndpointsByScheme)
            {
                // If there is only one endpoint for a given scheme, we use the scheme as the endpoint name
                // Otherwise, we use the actual endpoint names from the config
                var schemeAsEndpointName = schemeGroup.Count() <= 1 ? schemeGroup.Key : null;

                foreach (var endpoint in schemeGroup)
                {
                    builder.WithEndpoint(schemeAsEndpointName ?? endpoint.EndpointName, e =>
                    {
                        e.Port = endpoint.BindingAddress.Port;
                        e.UriScheme = endpoint.BindingAddress.Scheme;
                        e.IsProxied = false; // turn off the proxy, as we cannot easily override Kestrel bindings
                        e.Transport = adjustTransport(e);
                    },
                    createIfNotExists: true);
                }
            }

            // We don't need to set ASPNETCORE_URLS if we have Kestrel endpoints configured
            // as Kestrel will get everything it needs from the config.
            if (!kestrelEndpointsByScheme.Any())
            {
                builder.SetAspNetCoreUrls();
            }

            // Process the launch profile and turn it into environment variables and endpoints.
            if (launchProfile is null)
            {
                return builder;
            }

            // If we had found any Kestrel endpoints, we ignore the launch profile endpoints,
            // to match the Kestrel runtime behavior.
            if (!kestrelEndpointsByScheme.Any())
            {
                var urlsFromApplicationUrl = launchProfile.ApplicationUrl?.Split(';', StringSplitOptions.RemoveEmptyEntries) ?? [];
                foreach (var url in urlsFromApplicationUrl)
                {
                    var uri = new Uri(url);

                    builder.WithEndpoint(uri.Scheme, e =>
                    {
                        e.Port = uri.Port;
                        e.UriScheme = uri.Scheme;
                        e.FromLaunchProfile = true;
                        e.Transport = adjustTransport(e);
                    },
                    createIfNotExists: true);
                }
            }

            builder.WithEnvironment(context =>
            {
                // Populate DOTNET_LAUNCH_PROFILE environment variable for consistency with "dotnet run" and "dotnet watch".
                if (effectiveLaunchProfile is not null)
                {
                    context.EnvironmentVariables.TryAdd("DOTNET_LAUNCH_PROFILE", effectiveLaunchProfile.Name);
                }

                foreach (var envVar in launchProfile.EnvironmentVariables)
                {
                    var value = Environment.ExpandEnvironmentVariables(envVar.Value);
                    context.EnvironmentVariables.TryAdd(envVar.Key, value);
                }
            });

            // NOTE: the launch profile command line arguments will be processed by ApplicationExecutor.PrepareProjectExecutables() (either by the IDE or manually passed to run)
        }
        else
        {
            // If we aren't a web project (looking at both launch profile and Kestrel config) we don't automatically add bindings.
            if (launchProfile?.ApplicationUrl == null && !kestrelEndpointsByScheme.Any())
            {
                return builder;
            }

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "http" || string.Equals(sb.Name, "http", StringComparisons.EndpointAnnotationName)))
            {
                builder.WithEndpoint("http", e =>
                {
                    e.UriScheme = "http";
                    e.Transport = adjustTransport(e);
                },
                createIfNotExists: true);
            }

            if (!projectResource.Annotations.OfType<EndpointAnnotation>().Any(sb => sb.UriScheme == "https" || string.Equals(sb.Name, "https", StringComparisons.EndpointAnnotationName)))
            {
                builder.WithEndpoint("https", e =>
                {
                    e.UriScheme = "https";
                    e.Transport = adjustTransport(e);
                },
                createIfNotExists: true);
            }
        }

        return builder;
    }

    /// <summary>
    /// Configures how many replicas of the project should be created for the project.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <param name="replicas">The number of replicas.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// When this method is applied to a project resource it will configure the app host to start multiple instances
    /// of the application based on the specified number of replicas. By default the app host automatically starts a
    /// reverse proxy for each process. When <see cref="WithReplicas(IResourceBuilder{ProjectResource}, int)"/> is
    /// used the reverse proxy will load balance traffic between the replicas.
    /// </para>
    /// <para>
    /// This capability can be useful when debugging scale out scenarios to ensure state is appropriately managed
    /// within a cluster of instances.
    /// </para>
    /// </remarks>
    /// <example>
    /// Start multiple instances of the same service.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddProject&lt;Projects.InventoryService&gt;("inventoryservice")
    ///        .WithReplicas(3);
    /// </code>
    /// </example>
    public static IResourceBuilder<ProjectResource> WithReplicas(this IResourceBuilder<ProjectResource> builder, int replicas)
    {
        builder.WithAnnotation(new ReplicaAnnotation(replicas));
        return builder;
    }

    /// <summary>
    /// Configures the project to disable forwarded headers when being published.
    /// </summary>
    /// <param name="builder">The project resource builder.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// <para>
    /// By default .NET Aspire assumes that .NET applications which expose endpoints should be configured to
    /// use forwarded headers. This is because most typical cloud native deployment scenarios involve a reverse
    /// proxy which translates an external endpoint hostname to an internal address.
    /// </para>
    /// <para>
    /// To enable forwarded headers the <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c> variable is injected
    /// into the project and set to true. If the <see cref="DisableForwardedHeaders(IResourceBuilder{ProjectResource})"/>
    /// extension is used this environment variable will not be set.
    /// </para>
    /// </remarks>
    /// <example>
    /// Disable forwarded headers for a project.
    /// <code lang="C#">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// builder.AddProject&lt;Projects.InventoryService&gt;("inventoryservice")
    ///        .DisableForwardedHeaders();
    /// </code>
    /// </example>
    public static IResourceBuilder<ProjectResource> DisableForwardedHeaders(this IResourceBuilder<ProjectResource> builder)
    {
        builder.WithAnnotation<DisableForwardedHeadersAnnotation>(ResourceAnnotationMutationBehavior.Replace);
        return builder;
    }

    private static IConfiguration GetConfiguration(ProjectResource projectResource)
    {
        var projectMetadata = projectResource.GetProjectMetadata();

        // For testing
        if (projectMetadata.Configuration is { } configuration)
        {
            return configuration;
        }

        var projectDirectoryPath = Path.GetDirectoryName(projectMetadata.ProjectPath)!;
        var appSettingsPath = Path.Combine(projectDirectoryPath, "appsettings.json");
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT");
        var appSettingsEnvironmentPath = Path.Combine(projectDirectoryPath, $"appsettings.{env}.json");

        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddJsonFile(appSettingsPath, optional: true);
        configBuilder.AddJsonFile(appSettingsEnvironmentPath, optional: true);
        return configBuilder.Build();
    }

    private static void SetAspNetCoreUrls(this IResourceBuilder<ProjectResource> builder)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsPublishMode)
        {
            return;
        }

        builder.WithEnvironment(context =>
        {
            if (context.EnvironmentVariables.ContainsKey("ASPNETCORE_URLS"))
            {
                // If the user has already set ASPNETCORE_URLS, we don't want to override it.
                return;
            }

            var aspnetCoreUrls = new ReferenceExpressionBuilder();

            var processedHttpsPort = false;
            var first = true;

            static bool IsValidAspNetCoreUrl(EndpointAnnotation e) =>
                e.UriScheme is "http" or "https" && e.TargetPortEnvironmentVariable is null;

            // Turn http and https endpoints into a single ASPNETCORE_URLS environment variable.
            foreach (var e in builder.Resource.GetEndpoints().Where(e => IsValidAspNetCoreUrl(e.EndpointAnnotation)))
            {
                if (!first)
                {
                    aspnetCoreUrls.AppendLiteral(";");
                }

                if (!processedHttpsPort && e.EndpointAnnotation.UriScheme == "https")
                {
                    // Add the environment variable for the HTTPS port if we have an HTTPS service. This will make sure the
                    // HTTPS redirection middleware avoids redirecting to the internal port.
                    context.EnvironmentVariables["ASPNETCORE_HTTPS_PORT"] = e.Property(EndpointProperty.Port);

                    processedHttpsPort = true;
                }

                aspnetCoreUrls.Append($"{e.Property(EndpointProperty.Scheme)}://localhost:{e.Property(EndpointProperty.TargetPort)}");
                first = false;
            }

            if (!aspnetCoreUrls.IsEmpty)
            {
                // Combine into a single expression
                context.EnvironmentVariables["ASPNETCORE_URLS"] = aspnetCoreUrls.Build();
            }
        });
    }
}
