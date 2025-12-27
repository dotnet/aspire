// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;

namespace Aspire.Cli.Projects;

/// <summary>
/// Context for adding a package to an AppHost project.
/// </summary>
internal sealed class AddPackageContext
{
    /// <summary>
    /// Gets or sets the AppHost file.
    /// </summary>
    public required FileInfo AppHostFile { get; init; }

    /// <summary>
    /// Gets or sets the package ID to add.
    /// </summary>
    public required string PackageId { get; init; }

    /// <summary>
    /// Gets or sets the package version to add.
    /// </summary>
    public required string PackageVersion { get; init; }

    /// <summary>
    /// Gets or sets the optional NuGet source.
    /// </summary>
    public string? Source { get; init; }
}

/// <summary>
/// Context for publishing an AppHost project.
/// </summary>
internal sealed class PublishContext
{
    /// <summary>
    /// Gets the AppHost file to publish.
    /// </summary>
    public required FileInfo AppHostFile { get; init; }

    /// <summary>
    /// Gets the detected type of the AppHost.
    /// </summary>
    public required AppHostType Type { get; init; }

    /// <summary>
    /// Gets the output path for publish artifacts.
    /// </summary>
    public string? OutputPath { get; init; }

    /// <summary>
    /// Gets additional environment variables to pass to the AppHost.
    /// </summary>
    public IDictionary<string, string> EnvironmentVariables { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets the arguments to pass to the AppHost for publishing.
    /// </summary>
    public string[] Arguments { get; init; } = [];

    /// <summary>
    /// Gets the task completion source for the backchannel connection.
    /// </summary>
    public TaskCompletionSource<IAppHostCliBackchannel>? BackchannelCompletionSource { get; init; }

    /// <summary>
    /// Gets the working directory for the command.
    /// </summary>
    public required DirectoryInfo WorkingDirectory { get; init; }
}

/// <summary>
/// Interface for AppHost projects of various types.
/// </summary>
internal interface IAppHostProject
{
    /// <summary>
    /// Gets the AppHost type that this runner supports.
    /// </summary>
    AppHostType SupportedType { get; }

    /// <summary>
    /// Runs the AppHost project.
    /// </summary>
    /// <param name="context">The context containing all information needed to run the AppHost.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The exit code from running the AppHost.</returns>
    Task<int> RunAsync(AppHostProjectContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Publishes the AppHost project.
    /// </summary>
    /// <param name="context">The context containing all information needed to publish the AppHost.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The exit code from publishing the AppHost.</returns>
    Task<int> PublishAsync(PublishContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Validates that the AppHost file is compatible with this runner.
    /// </summary>
    /// <param name="appHostFile">The AppHost file to validate.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the AppHost is valid and compatible; otherwise, false.</returns>
    Task<bool> ValidateAsync(FileInfo appHostFile, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a package to the AppHost project.
    /// </summary>
    /// <param name="context">The context containing package information.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>True if the package was added successfully; otherwise, false.</returns>
    Task<bool> AddPackageAsync(AddPackageContext context, CancellationToken cancellationToken);
}
