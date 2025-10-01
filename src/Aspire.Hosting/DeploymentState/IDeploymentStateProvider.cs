// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;

namespace Aspire.Hosting.DeploymentState;

/// <summary>
/// Provides functionality for loading and saving deployment state.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IDeploymentStateProvider
{
    /// <summary>
    /// Loads the deployment state.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the deployment state as a JSON object.</returns>
    Task<JsonObject> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the deployment state.
    /// </summary>
    /// <param name="state">The deployment state to save.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SaveAsync(JsonObject state, CancellationToken cancellationToken = default);
}
