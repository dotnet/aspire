// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Aspire.Hosting.Dcp.Model;
using HealthStatus = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An immutable snapshot of the state of a resource.
/// </summary>
public sealed record CustomResourceSnapshot
{
    /// <summary>
    /// The type of the resource.
    /// </summary>
    public required string ResourceType { get; init; }

    /// <summary>
    /// The properties that should show up in the dashboard for this resource.
    /// </summary>
    public required ImmutableArray<ResourcePropertySnapshot> Properties { get; init; }

    /// <summary>
    /// The creation timestamp of the resource.
    /// </summary>
    public DateTime? CreationTimeStamp { get; init; }

    /// <summary>
    /// The start timestamp of the resource.
    /// </summary>
    public DateTime? StartTimeStamp { get; init; }

    /// <summary>
    /// The stop timestamp of the resource.
    /// </summary>
    public DateTime? StopTimeStamp { get; init; }

    /// <summary>
    /// Represents the state of the resource.
    /// </summary>
    public ResourceStateSnapshot? State { get; init; }

    /// <summary>
    /// The exit code of the resource.
    /// </summary>
    public int? ExitCode { get; init; }

    /// <summary>
    /// Gets the health status of the resource.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This value is derived from <see cref="HealthReports"/>. However, if a resource
    /// is known to have a health check and no reports exist, then this value is <see langword="null"/>.
    /// </para>
    /// <para>
    /// Defaults to <see cref="HealthStatus.Healthy"/>.
    /// </para>
    /// </remarks>
    public HealthStatus? HealthStatus { get; init; } = Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy;

    /// <summary>
    /// The health reports for this resource.
    /// </summary>
    /// <remarks>
    /// May be zero or more. If there are no health reports, the resource is considered healthy
    /// so long as no heath checks are registered for the resource.
    /// </remarks>
    public ImmutableArray<HealthReportSnapshot> HealthReports { get; init; } = [];

    /// <summary>
    /// The environment variables that should show up in the dashboard for this resource.
    /// </summary>
    public ImmutableArray<EnvironmentVariableSnapshot> EnvironmentVariables { get; init; } = [];

    /// <summary>
    /// The URLs that should show up in the dashboard for this resource.
    /// </summary>
    public ImmutableArray<UrlSnapshot> Urls { get; init; } = [];

    /// <summary>
    /// The volumes that should show up in the dashboard for this resource.
    /// </summary>
    public ImmutableArray<VolumeSnapshot> Volumes { get; init; } = [];

    /// <summary>
    /// The commands available in the dashboard for this resource.
    /// </summary>
    public ImmutableArray<ResourceCommandSnapshot> Commands { get; init; } = [];

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable RS0016 // Add public types and members to the declared API
    public ImmutableArray<RelationshipSnapshot> Relationships { get; init; } = [];
#pragma warning restore RS0016 // Add public types and members to the declared API
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}

/// <summary>
/// A snapshot of the resource state
/// </summary>
/// <param name="Text">The text for the state update. See <see cref="KnownResourceStates"/> for expected values.</param>
/// <param name="Style">The style for the state update. Use <seealso cref="KnownResourceStateStyles"/> for the supported styles.</param>
public sealed record ResourceStateSnapshot(string Text, string? Style)
{
    /// <summary>
    /// Convert text to state snapshot. The style will be null by default
    /// </summary>
    /// <param name="s"></param>
    public static implicit operator ResourceStateSnapshot?(string? s) =>
        s is null ? null : new(Text: s, Style: null);
}

/// <summary>
/// A snapshot of an environment variable.
/// </summary>
/// <param name="Name">The name of the environment variable.</param>
/// <param name="Value">The value of the environment variable.</param>
/// <param name="IsFromSpec">Determines if this environment variable was defined in the resource explicitly or computed (for e.g. inherited from the process hierarchy).</param>
public sealed record EnvironmentVariableSnapshot(string Name, string? Value, bool IsFromSpec);

/// <summary>
/// A snapshot of the url.
/// </summary>
/// <param name="Name">Name of the url.</param>
/// <param name="Url">The full uri.</param>
/// <param name="IsInternal">Determines if this url is internal.</param>
public sealed record UrlSnapshot(string Name, string Url, bool IsInternal);

/// <summary>
/// A snapshot of a volume, mounted to a container.
/// </summary>
/// <param name="Source">The name of the volume. Can be <see langword="null"/> if the mount is an anonymous volume.</param>
/// <param name="Target">The target of the mount.</param>
/// <param name="MountType">Gets the mount type, such as <see cref="VolumeMountType.Bind"/> or <see cref="VolumeMountType.Volume"/></param>
/// <param name="IsReadOnly">Whether the volume mount is read-only or not.</param>
public sealed record VolumeSnapshot(string? Source, string Target, string MountType, bool IsReadOnly);

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
#pragma warning disable RS0016 // Add public types and members to the declared API
public sealed record RelationshipSnapshot(string ResourceName, string Type, ImmutableDictionary<string, object> Properties);
#pragma warning restore RS0016 // Add public types and members to the declared API
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

/// <summary>
/// A snapshot of the resource property.
/// </summary>
/// <param name="Name">The name of the property.</param>
/// <param name="Value">The value of the property.</param>
public sealed record ResourcePropertySnapshot(string Name, object? Value)
{
    /// <summary>
    /// Whether this property is considered sensitive or not.
    /// </summary>
    /// <remarks>
    /// Sensitive properties are masked when displayed in UI and require an explicit user action to reveal.
    /// </remarks>
    public bool IsSensitive { get; init; }

    internal void Deconstruct(out string name, out object? value, out bool isSensitive)
    {
        name = Name;
        value = Value;
        isSensitive = IsSensitive;
    }
}

/// <summary>
/// A snapshot of a resource command.
/// </summary>
/// <param name="Type">The type of command. The type uniquely identifies the command.</param>
/// <param name="State">The state of the command.</param>
/// <param name="DisplayName">The display name visible in UI for the command.</param>
/// <param name="DisplayDescription">
/// Optional description of the command, to be shown in the UI.
/// Could be used as a tooltip. May be localized.
/// </param>
/// <param name="Parameter">
/// Optional parameter that configures the command in some way.
/// Clients must return any value provided by the server when invoking the command.
/// </param>
/// <param name="ConfirmationMessage">
/// When a confirmation message is specified, the UI will prompt with an OK/Cancel dialog
/// and the confirmation message before starting the command.
/// </param>
/// <param name="IconName">The icon name for the command. The name should be a valid FluentUI icon name. https://aka.ms/fluentui-system-icons</param>
/// <param name="IconVariant">The icon variant.</param>
/// <param name="IsHighlighted">A flag indicating whether the command is highlighted in the UI.</param>
public sealed record ResourceCommandSnapshot(string Type, ResourceCommandState State, string DisplayName, string? DisplayDescription, object? Parameter, string? ConfirmationMessage, string? IconName, IconVariant? IconVariant, bool IsHighlighted);

/// <summary>
/// A report produced by a health check about a resource.
/// </summary>
/// <param name="Name">The name of the health check that produced this report.</param>
/// <param name="Status">The state of the resource, according to the report.</param>
/// <param name="Description">An optional description of the report, for display.</param>
/// <param name="ExceptionText">An optional string containing exception details.</param>
public sealed record HealthReportSnapshot(string Name, HealthStatus Status, string? Description, string? ExceptionText);

/// <summary>
/// The state of a resource command.
/// </summary>
public enum ResourceCommandState
{
    /// <summary>
    /// Command is visible and enabled for use.
    /// </summary>
    Enabled,
    /// <summary>
    /// Command is visible and disabled for use.
    /// </summary>
    Disabled,
    /// <summary>
    /// Command is hidden.
    /// </summary>
    Hidden
}

/// <summary>
/// The set of well known resource states.
/// </summary>
public static class KnownResourceStateStyles
{
    /// <summary>
    /// The success state
    /// </summary>
    public static readonly string Success = "success";

    /// <summary>
    /// The error state. Useful for error messages.
    /// </summary>
    public static readonly string Error = "error";

    /// <summary>
    /// The info state. Useful for informational messages.
    /// </summary>
    public static readonly string Info = "info";

    /// <summary>
    /// The warn state. Useful for showing warnings.
    /// </summary>
    public static readonly string Warn = "warn";
}

/// <summary>
/// The set of well known resource states.
/// </summary>
public static class KnownResourceStates
{
    /// <summary>
    /// The hidden state. Useful for hiding the resource.
    /// </summary>
    public static readonly string Hidden = nameof(Hidden);

    /// <summary>
    /// The starting state. Useful for showing the resource is starting.
    /// </summary>
    public static readonly string Starting = nameof(Starting);

    /// <summary>
    /// The running state. Useful for showing the resource is running.
    /// </summary>
    public static readonly string Running = nameof(Running);

    /// <summary>
    /// The finished state. Useful for showing the resource has failed to start successfully.
    /// </summary>
    public static readonly string FailedToStart = nameof(FailedToStart);

    /// <summary>
    /// The stopping state. Useful for showing the resource is stopping.
    /// </summary>
    public static readonly string Stopping = nameof(Stopping);

    /// <summary>
    /// The exited state. Useful for showing the resource has exited.
    /// </summary>
    public static readonly string Exited = nameof(Exited);

    /// <summary>
    /// The finished state. Useful for showing the resource has finished.
    /// </summary>
    public static readonly string Finished = nameof(Finished);

    /// <summary>
    /// The waiting state. Useful for showing the resource is waiting for a dependency.
    /// </summary>
    public static readonly string Waiting = nameof(Waiting);

    /// <summary>
    /// List of terminal states.
    /// </summary>
    public static readonly IReadOnlyList<string> TerminalStates = [Finished, FailedToStart, Exited];
}

internal static class ResourceSnapshotBuilder
{
    public static ImmutableArray<RelationshipSnapshot> BuildRelationships(IResource resource)
    {
        var relationships = ImmutableArray.CreateBuilder<RelationshipSnapshot>();

        foreach (var annotation in resource.Annotations.OfType<ResourceRelationshipAnnotation>())
        {
            relationships.Add(new(annotation.Resource.Name, annotation.Type, annotation.Properties.ToImmutableDictionary()));
        }

        return relationships.ToImmutable();
    }
}
