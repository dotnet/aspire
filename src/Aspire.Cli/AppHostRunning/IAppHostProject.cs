// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.AppHostRunning;

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
