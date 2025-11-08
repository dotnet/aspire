// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Validates that the Node.js executable is available on the system.
/// </summary>
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class NodeInstallationManager : RequiredCommandValidator
{
    private string? _resolvedCommandPath;

    public NodeInstallationManager(
        IInteractionService interactionService,
        ILogger<NodeInstallationManager> logger)
        : base(interactionService, logger)
    {
    }

    /// <summary>
    /// Ensures Node.js is installed/available. This method is safe for concurrent callers;
    /// only one validation will run at a time.
    /// </summary>
    /// <param name="throwOnFailure">Whether to throw an exception if Node.js is not found. Default is true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task EnsureInstalledAsync(bool throwOnFailure = true, CancellationToken cancellationToken = default)
    {
        SetThrowOnFailure(throwOnFailure);
        return RunAsync(cancellationToken);
    }

    protected override string GetCommandPath() => "node";

    protected override Task OnValidatedAsync(string resolvedCommandPath, CancellationToken cancellationToken)
    {
        _resolvedCommandPath = resolvedCommandPath;
        return Task.CompletedTask;
    }

    protected override string? GetHelpLink() => "https://nodejs.org/en/download/";
}
#pragma warning restore ASPIREINTERACTION001
