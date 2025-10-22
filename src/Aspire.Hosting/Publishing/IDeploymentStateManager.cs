// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Provides deployment state management functionality.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IDeploymentStateManager
{
    /// <summary>
    /// Gets the file path where deployment state is stored, if applicable.
    /// </summary>
    string? StateFilePath { get; }

    /// <summary>
    /// Acquires a specific section of the deployment state with version tracking for concurrency control.
    /// The returned StateSection should be disposed after use to release the section lock.
    /// </summary>
    /// <param name="sectionName">The name of the section to acquire (e.g., "Parameters", "Azure").</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A StateSection containing the section data and version information.</returns>
    Task<DeploymentStateSection> AcquireSectionAsync(string sectionName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a section of deployment state with optimistic concurrency control.
    /// The section must be saved before it is disposed or modified by another operation.
    /// </summary>
    /// <param name="section">The section to save, including version information.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <exception cref="InvalidOperationException">Thrown when a version conflict is detected, indicating the section was modified after it was acquired.</exception>
    Task SaveSectionAsync(DeploymentStateSection section, CancellationToken cancellationToken = default);
}
