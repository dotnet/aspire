// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Text.Json.Serialization;
using k8s.Models;

namespace Aspire.Hosting.Dcp.Model;

internal sealed class ContainerSpec
{
    // Image to be used to create the container
    [JsonPropertyName("image")]
    public string? Image { get; set; }

    // Volumes that should be mounted into the container
    [JsonPropertyName("volumeMounts")]
    public List<VolumeMount>? VolumeMounts { get; set; }

    // Exposed ports
    [JsonPropertyName("ports")]
    public List<ContainerPortSpec>? Ports { get; set; }

    // Environment variables to be used for the container
    [JsonPropertyName("env")]
    public List<EnvVar>? Env { get; set; }

    // Environment files to use to populate Container environment during startup
    [JsonPropertyName("envFiles")]
    public List<string>? EnvFiles { get; set; }

    // Container restart policy
    [JsonPropertyName("restartPolicy")]
    public string? RestartPolicy { get; set; } = ContainerRestartPolicy.None;

    // Command to run in the container (entrypoint)
    [JsonPropertyName("command")]
    public string? Command { get; set; }

    // Arguments to pass to the command that starts the container
    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }
}

internal static class VolumeMountType
{
    // A volume mount to a host directory
    public const string Bind = "bind";

    // A volume mount to a named volume managed by the container orchestrator
    public const string Named = "volume";
}

internal sealed class VolumeMount
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = VolumeMountType.Bind;

    // Bind mounts: the host directory to mount
    // Volume mounts: name of the volume to mount
    [JsonPropertyName("source")]
    public string? Source { get; set; }

    // The path within the container that the mount will use
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    // True if the mounted file system is supposed to be read-only
    [JsonPropertyName("readOnly")]
    public bool IsReadOnly { get; set; } = false;
}

internal static class ContainerRestartPolicy
{
    // Do not automatically restart the container when it exits (default)
    public const string None = "no";

    // Restart only if the container exits with non-zero status
    public const string OnFailure = "on-failure";

    // Restart container, except if container is explicitly stopped (or container daemon is stopped/restarted)
    public const string UnlessStopped = "unless-stopped";

    // Always try to restart the container
    public const string Always = "always";
}

internal static class PortProtocol
{
    public const string TCP = "TCP";

    public const string UDP = "UDP";

    public static string Canonicalize(string protocol)
    {
        var protocolUC = protocol.ToUpperInvariant();
        switch (protocolUC)
        {
            case TCP:
            case UDP:
                return protocolUC;
            default:
                throw new ArgumentException("Port protocol value must be 'TCP' or 'UDP'");
        }
    }

    public static ProtocolType ToProtocolType(string protocol)
    {
        var canonical = Canonicalize(protocol);
        switch (canonical)
        {
            case TCP:
                return ProtocolType.Tcp;
            case UDP:
                return ProtocolType.Udp;
            default:
                throw new ArgumentException("Supported protocols are TCP and UDP");
        }
    }

    public static string FromProtocolType(ProtocolType protocolType)
    {
        switch (protocolType)
        {
            case ProtocolType.Tcp:
                return TCP;
            case ProtocolType.Udp:
                return UDP;
            default:
                throw new ArgumentException("Supported protocols are TCP and UDP");
        }
    }
}

internal sealed class ContainerPortSpec
{
    // Optional: If specified, this must be a valid port number, 0 < x < 65536.
    [JsonPropertyName("hostPort")]
    public int? HostPort { get; set; }

    // Required: This must be a valid port number, 0 < x < 65536.
    [JsonPropertyName("containerPort")]
    public int? ContainerPort { get; set; }

    // The network protocol to be used, defaults to TCP
    [JsonPropertyName("protocol")]
    public string Protocol { get; set; } = PortProtocol.TCP;

    // Optional: What host IP to bind the external port to.
    [JsonPropertyName("hostIP")]
    public string? HostIP { get; set; }
}

internal sealed class ContainerStatus : V1Status
{
    // Current state of the Container.
    [JsonPropertyName("state")]
    public string? State { get; set; }

    // ID of the Container (if an attempt to start the Container was made)
    [JsonPropertyName("containerId")]
    public string? ContainerId { get; set; }

    // Timestamp of the Container start attempt
    [JsonPropertyName("startupTimestamp")]
    public DateTimeOffset? StartupTimestamp { get; set; }

    // Timestamp when the Container was terminated last
    [JsonPropertyName("finishTimestamp")]
    public DateTimeOffset? FinishTimestamp { get; set; }

    // Exit code of the Container.
    // Default is -1, meaning the exit code is not known, or the container is still running.
    [JsonPropertyName("exitCode")]
    public int ExitCode { get; set; } = Conventions.UnknownExitCode;

    // Note: the ContainerStatus has "Message" property that represents a human-readable information about Container state.
    // It is provided by V1Status base class.
}

internal static class ContainerState
{
    // Pending is the initial Container state. No attempt has been made to run the container yet.
    public const string Pending = "Pending";

    // A start attempt was made, but it failed
    public const string FailedToStart = "FailedToStart";

    // Container has been started and is executing
    public const string Running = "Running";

    // Container is paused
    public const string Paused = "Paused";

    // Container finished execution
    public const string Exited = "Exited";

    // Container was running at some point, but has been removed.
    public const string Removed = "Removed";

    // Unknown means for some reason container state is unavailable.
    public const string Unknown = "Unknown";
}

internal sealed class Container : CustomResource<ContainerSpec, ContainerStatus>
{
    [JsonConstructor]
    public Container(ContainerSpec spec) : base(spec) { }

    public static Container Create(string name, string image)
    {
        var c = new Container(new ContainerSpec { Image = image });

        c.Kind = Dcp.ContainerKind;
        c.ApiVersion = Dcp.GroupVersion.ToString();
        c.Metadata.Name = name;
        c.Metadata.NamespaceProperty = string.Empty;

        return c;
    }
}

