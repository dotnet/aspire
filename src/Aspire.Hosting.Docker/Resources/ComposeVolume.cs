// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Yaml;

namespace Aspire.Hosting.Docker.Resources;

/// <summary>
/// Represents a Docker Compose volume definition within a YAML configuration.
/// </summary>
/// <remarks>
/// The <c>ComposeVolume</c> class is a representation of a volume element in a Docker Compose file.
/// It extends the <c>YamlObject</c> class, allowing manipulation and serialization of YAML structures.
/// This class facilitates adding configuration options to a volume, such as specifying whether the volume is external.
/// </remarks>
public sealed class ComposeVolume : YamlObject
{
    /// <summary>
    /// Represents a Docker Compose volume in the YAML configuration.
    /// Used to define source, target, and configuration settings for a volume in a containerized environment.
    /// </summary>
    /// <param name="source">Optional source for the volume.</param>
    /// <param name="target">Optional target for the volume.</param>
    /// <param name="type">Optional mount type for the volume.</param>
    /// <param name="external">Optionally set if the volume is external or not.</param>
    /// <param name="readOnly">Optionally set if the volume is read_only or not.</param>
    public ComposeVolume(string? source = null, string? target = null, ContainerMountType? type = null, bool? external = null, bool? readOnly = null)
    {
        InitializeMountTypeIfSet(type);
        InitializeReadOnlyIfSet(readOnly);
        InitializeSourceIfSet(source);
        InitializeTargetIfSet(target);
        External = external;
    }

    /// <summary>
    /// Gets the source path or name of the volume defined in the Docker Compose configuration.
    /// </summary>
    /// <remarks>
    /// The Source property specifies the host path or volume name for the Docker volume.
    /// In case of a bind mount, this represents the absolute path on the host machine.
    /// For named volumes, it denotes the volume name as defined in the Docker Compose file.
    /// </remarks>
    public string? Source { get; private set; }

    /// <summary>
    /// Gets the destination path within the container where the volume is mounted, as defined in the Docker Compose configuration.
    /// </summary>
    /// <remarks>
    /// The Destination property specifies the container's internal file system path where the volume is mounted.
    /// It determines the target path inside the container where the data from the host or volume will be accessible.
    /// This is usually defined in the Docker Compose file and is essential for mapping persistent storage into a container's runtime environment.
    /// </remarks>
    public string? Target { get; private set;}

    /// <summary>
    /// Gets the mount type of the container volume as specified in the Docker Compose configuration.
    /// </summary>
    /// <remarks>
    /// The Type property indicates whether the volume is a bind mount or a named volume.
    /// This information is derived from the <c>ContainerMountType</c> enumeration,
    /// allowing clear differentiation between the types of mounts and their intended usage.
    /// </remarks>
    public ContainerMountType? Type { get; private set;}

    /// <summary>
    /// Gets a value indicating whether the volume is mounted as read-only in the Docker Compose configuration.
    /// </summary>
    /// <remarks>
    /// The ReadOnly property specifies if the volume is restricted to read-only access when mounted to a container.
    /// If set to true, the container cannot modify the volume's data.
    /// </remarks>
    public bool? ReadOnly { get; private set;}

    /// <summary>
    /// Indicates whether the volume is external to the Docker Compose application.
    /// </summary>
    /// <remarks>
    /// The External property specifies if a volume is managed externally and not created automatically by Docker Compose.
    /// When this property is set to true, Docker Compose assumes the existence of the specified volume and does not attempt to create it.
    /// This is typically used for volumes that are pre-existing or managed outside of the application's lifecycle.
    /// </remarks>
    public bool? External { get; private set;}

    /// <summary>
    /// Configures the volume as external or not in the YAML configuration.
    /// </summary>
    /// <param name="external">A boolean value indicating whether the volume should be marked as external.</param>
    /// <returns>Returns the instance of <see cref="ComposeVolume"/> with the updated configuration.</returns>
    public ComposeVolume SetExternal(bool external)
    {
        Replace(DockerComposeYamlKeys.External, new YamlValue(external));
        External = external;
        return this;
    }

    /// <summary>
    /// Sets the read-only attribute for the volume.
    /// </summary>
    /// <param name="readOnly">A boolean value indicating whether the volume should be read-only.</param>
    /// <returns>The current instance of <c>ComposeVolume</c> with the updated read-only attribute.</returns>
    public ComposeVolume SetReadOnly(bool readOnly)
    {
        Replace(DockerComposeYamlKeys.ReadOnly, new YamlValue(readOnly));
        ReadOnly = readOnly;
        return this;
    }

    /// <summary>
    /// Sets the source path for the volume in the docker-compose YAML configuration.
    /// </summary>
    /// <param name="source">The source path to be used for the volume.</param>
    /// <returns>Returns the updated <see cref="ComposeVolume"/> instance with the specified source set.</returns>
    public ComposeVolume SetSource(string source)
    {
        Replace(DockerComposeYamlKeys.Source, new YamlValue(source));
        Source = source;
        return this;
    }

    /// <summary>
    /// Sets the target (destination path) for the volume in the container.
    /// </summary>
    /// <param name="target">The target path where the volume will be mounted inside the container.</param>
    /// <returns>The updated instance of <see cref="ComposeVolume"/> to allow method chaining.</returns>
    public ComposeVolume SetTarget(string target)
    {
        Replace(DockerComposeYamlKeys.Target, new YamlValue(target));
        Target = target;
        return this;
    }

    /// <summary>
    /// Sets the driver of the Compose volume to "local".
    /// </summary>
    /// <remarks>
    /// This method configures the volume to use the "local" driver, which manages storage on the host
    /// where the Docker daemon is running. By invoking this method, the "driver" property of the Compose
    /// volume is replaced with "local" in the YAML configuration.
    /// </remarks>
    /// <returns>
    /// Returns the updated <c>ComposeVolume</c> instance after applying the local driver configuration.
    /// </returns>
    public ComposeVolume SetLocalDriver()
    {
        Replace(DockerComposeYamlKeys.Driver, new YamlValue("local"));
        return this;
    }

    /// <summary>
    /// Sets the mount type for the Docker Compose volume.
    /// </summary>
    /// <param name="mountType">
    /// The type of the mount to be set. Valid values are <c>ContainerMountType.BindMount</c> and
    /// <c>ContainerMountType.Volume</c>.
    /// </param>
    /// <returns>
    /// Returns the current instance of <c>ComposeVolume</c> with the specified mount type configured.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the provided <paramref name="mountType"/> value is not a recognized <c>ContainerMountType</c>.
    /// </exception>
    public ComposeVolume SetMountType(ContainerMountType mountType)
    {
        switch (mountType)
        {
            case ContainerMountType.BindMount:
                Replace(DockerComposeYamlKeys.Type, new YamlValue("bind"));
                break;
            case ContainerMountType.Volume:
                Replace(DockerComposeYamlKeys.Type, new YamlValue("volume"));
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mountType), mountType, null);
        }

        Type = mountType;

        return this;
    }

    private void InitializeMountTypeIfSet(ContainerMountType? type)
    {
        if (!type.HasValue)
        {
            return;
        }

        SetMountType(type.Value);
    }

    private void InitializeTargetIfSet(string? target)
    {
        if (string.IsNullOrWhiteSpace(target))
        {
            return;
        }

        SetTarget(target);
    }

    private void InitializeSourceIfSet(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return;
        }

        SetSource(source);
    }

    private void InitializeReadOnlyIfSet(bool? readOnly)
    {
        if (!readOnly.HasValue)
        {
            return;
        }

        SetReadOnly(readOnly.Value);
    }
}
