// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding Seq server resources to the application model.
/// </summary>
public static class SeqBuilderExtensions
{
    // The path within the container in which Seq stores its data
    private const string SeqContainerDataDirectory = "/data";

    private const string AcceptEulaEnvVarName = "ACCEPT_EULA";
    private const string UserEnvVarName = "SEQ_FIRSTRUN_ADMINUSERNAME";
    private const string PasswordEnvVarName = "SEQ_FIRSTRUN_ADMINPASSWORD";

    /// <summary>
    /// Adds a Seq server resource to the application model. A container is used for local development.
    /// </summary>
    /// <remarks>
    /// This version of the package defaults to the <inheritdoc cref="SeqContainerImageTags.Tag"/> tag of the <inheritdoc cref="SeqContainerImageTags.Image"/> container image.
    /// </remarks>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name to give the resource.</param>
    /// <param name="port">The host port for the Seq server.</param>
#pragma warning disable RS0016 // Add public types and members to the declared API
    public static IResourceBuilder<SeqResource> AddSeq(
#pragma warning restore RS0016 // Add public types and members to the declared API
        this IDistributedApplicationBuilder builder,
        string name,
        int? port = null)
    {
        var seqResource = new SeqResource(name);
        var resourceBuilder = builder.AddResource(seqResource)
            .WithHttpEndpoint(port: port, targetPort: 80, name: SeqResource.PrimaryEndpointName)
            .WithImage(SeqContainerImageTags.Image, SeqContainerImageTags.Tag)
            .WithImageRegistry(SeqContainerImageTags.Registry)
            .WithEnvironment(AcceptEulaEnvVarName, "Y");

        return resourceBuilder;
    }

    /// <summary>
    /// Enable authentication, providing a username and password for the default admin user.
    /// </summary>
    /// <remarks>If container storage is persisted, the username and password will also be
    /// persisted and must be managed through the Seq web interface. This method will not enable
    /// authentication if the container has already been persisted without authentication enabled.</remarks>
    /// <param name="builder">The Seq resource builder.</param>
    /// <param name="username">A parameter containing a username for the default Seq admin user.</param>
    /// <param name="password">A parameter a password for the default admin user.</param>
    /// <exception cref="ArgumentException">The supplied username or password is blank.</exception>
    public static IResourceBuilder<SeqResource> WithAuthentication(
        this IResourceBuilder<SeqResource> builder,
        IResourceBuilder<ParameterResource> username,
        IResourceBuilder<ParameterResource> password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username.Resource.Value, nameof(username));
        ArgumentException.ThrowIfNullOrWhiteSpace(password.Resource.Value, nameof(username));
        return builder
            .WithEnvironment(UserEnvVarName, username.Resource.Value)
            .WithEnvironment(PasswordEnvVarName, password.Resource.Value);
    }

    /// <summary>
    /// Adds a named volume for the data folder to a Seq container resource.
    /// </summary>
    /// <param name="builder">The Seq resource builder.</param>
    /// <param name="name">The name of the volume. Defaults to an auto-generated name based on the application and resource names.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only volume.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SeqResource> WithDataVolume(this IResourceBuilder<SeqResource> builder, string? name = null, bool isReadOnly = false)
        => builder.WithVolume(name ?? VolumeNameGenerator.Generate(builder, "data"), SeqContainerDataDirectory, isReadOnly);

    /// <summary>
    /// Adds a bind mount for the data folder to a Seq container resource.
    /// </summary>
    /// <param name="builder">The Seq resource builder.</param>
    /// <param name="source">The source directory on the host to mount into the container.</param>
    /// <param name="isReadOnly">A flag that indicates if this is a read-only mount.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<SeqResource> WithDataBindMount(this IResourceBuilder<SeqResource> builder, string source, bool isReadOnly = false)
        => builder.WithBindMount(source, SeqContainerDataDirectory, isReadOnly);
}
