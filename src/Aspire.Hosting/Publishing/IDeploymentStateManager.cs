// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

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
    /// Loads deployment state from the current application.
    /// </summary>
    Task<JsonObject> LoadStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves deployment state to the current application.
    /// </summary>
    Task SaveStateAsync(JsonObject state, CancellationToken cancellationToken = default);
}
