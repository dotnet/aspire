// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Lifecycle;
using Aspire.Hosting.Ollama;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for adding Ollama to the application model.
/// </summary>
public static class OllamaBuilderExtensions
{
    /// <summary>
    /// Adds an Ollama resource to the application. A container is used for local development.
    /// </summary>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var ollama = builder.AddOllama("ollama");
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(ollama);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <remarks>
    /// This version the package defaults to the 0.1.46 tag of the ollama/ollama container image.
    /// The .NET client library uses the http port by default to communicate and this resource exposes that endpoint.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency</param>
    /// <param name="enableGpu">Whether to enable GPU support.</param>
    /// <param name="port">The host port of the http endpoint of Ollama resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{OllamaResource}"/>.</returns>
    public static IResourceBuilder<OllamaResource> AddOllama(this IDistributedApplicationBuilder builder,
        string name,
        bool enableGpu = false,
        int? port = null)
    {
        builder.Services.TryAddLifecycleHook<OllamaLifecycleHook>();

        var ollama = new OllamaResource(name);

        var resource = builder.AddResource(ollama)
            .WithImage(OllamaContainerImageTags.Image)
            .WithImageRegistry(OllamaContainerImageTags.Registry)
            .WithHttpEndpoint(port: port, targetPort: 11434, OllamaResource.PrimaryEndpointName)
            .ExcludeFromManifest()
            .PublishAsContainer();

        if (enableGpu)
        {
            resource = resource.WithContainerRuntimeArgs("--gpus=all");
        }

        return resource;
    }

    /// <summary>
    /// Adds a model to the Ollama resource.
    /// </summary>
    /// <example>
    /// Use in application host
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var ollama = builder.AddOllama("ollama");
    ///   .AddModel("phi3")
    ///   .WithDataVolume("ollama");
    /// 
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(ollama);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <param name="builder">The Ollama resource builder.</param>
    /// <param name="modelName">The name of the model.</param>
    /// <remarks>This method will attempt to pull/download the model into the Ollama instance.</remarks>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OllamaResource> AddModel(this IResourceBuilder<OllamaResource> builder, string modelName)
    {
        builder.Resource.AddModel(modelName);
        return builder;
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Ollama container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the resource name.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OllamaResource> WithDataVolume(this IResourceBuilder<OllamaResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.CreateVolumeName(builder, "data"), "/root/.ollama", isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to an Ollama container resource.
    /// </summary>
    /// <param name="builder">The resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<OllamaResource> WithDataBindMount(this IResourceBuilder<OllamaResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, "/root/.ollama", isReadOnly);

    /// <summary>
    /// Adds an administration web UI Ollama to the application model using Attu. This version the package defaults to the main tag of the Open WebUI container image
    /// </summary>
    /// <example>
    /// Use in application host with an Ollama resource
    /// <code lang="csharp">
    /// var builder = DistributedApplication.CreateBuilder(args);
    ///
    /// var ollama = builder.AddOllama("ollama")
    ///   .WithOpenWebUI();
    /// var api = builder.AddProject&lt;Projects.Api&gt;("api")
    ///   .WithReference(ollama);
    ///  
    /// builder.Build().Run(); 
    /// </code>
    /// </example>
    /// <param name="builder">The Ollama resource builder.</param>
    /// <param name="configureContainer">Configuration callback for Open WebUI container resource.</param>
    /// <param name="containerName">The name of the container (Optional).</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>See https://openwebui.com for more information about Open WebUI</remarks>
    public static IResourceBuilder<T> WithOpenWebUI<T>(this IResourceBuilder<T> builder, Action<IResourceBuilder<OpenWebUIResource>>? configureContainer = null, string? containerName = null) where T : OllamaResource
    {
        containerName ??= $"{builder.Resource.Name}-openwebui";

        var openWebUI = new OpenWebUIResource(containerName);
        var resourceBuilder = builder.ApplicationBuilder.AddResource(openWebUI)
                                                        .WithImage(OllamaContainerImageTags.OpenWebUIImage, OllamaContainerImageTags.OpenWebUITag)
                                                        .WithImageRegistry(OllamaContainerImageTags.OpenWebUIRegistry)
                                                        .WithHttpEndpoint(targetPort: 8080, name: "http")
                                                        .WithVolume("open-webui","/app/backend/data")
                                                        .WithEnvironment(context => ConfigureOpenWebUIContainer(context, builder.Resource))
                                                        .ExcludeFromManifest();

        configureContainer?.Invoke(resourceBuilder);

        return builder;
    }

    private static void ConfigureOpenWebUIContainer(EnvironmentCallbackContext context, OllamaResource resource)
    {
        context.EnvironmentVariables.Add("ENABLE_SIGNUP", "false");
        context.EnvironmentVariables.Add("ENABLE_COMMUNITY_SHARING", "false"); // by default don't enable sharing
        context.EnvironmentVariables.Add("WEBUI_AUTH", "false"); // https://docs.openwebui.com/#quick-start-with-docker--recommended
        context.EnvironmentVariables.Add("OLLAMA_BASE_URL", $"{resource.PrimaryEndpoint.Scheme}://{resource.PrimaryEndpoint.ContainerHost}:{resource.PrimaryEndpoint.Port}");
    }
}
