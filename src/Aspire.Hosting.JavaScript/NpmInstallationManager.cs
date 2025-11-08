// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.JavaScript;

/// <summary>
/// Validates that the npm command is available on the system.
/// </summary>
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class NpmInstallationManager : RequiredCommandValidator
{
    private string? _resolvedCommandPath;

    public NpmInstallationManager(
        IInteractionService interactionService,
        ILogger<NpmInstallationManager> logger)
        : base(interactionService, logger)
    {
    }

    /// <summary>
    /// Ensures npm is installed/available. This method is safe for concurrent callers;
    /// only one validation will run at a time.
    /// </summary>
    /// <param name="throwOnFailure">Whether to throw an exception if npm is not found. Default is true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task EnsureInstalledAsync(bool throwOnFailure = true, CancellationToken cancellationToken = default)
    {
        SetThrowOnFailure(throwOnFailure);
        return RunAsync(cancellationToken);
    }

    protected override string GetCommandPath() => "npm";

    protected override Task OnValidatedAsync(string resolvedCommandPath, CancellationToken cancellationToken)
    {
        _resolvedCommandPath = resolvedCommandPath;
        return Task.CompletedTask;
    }

    protected override string? GetHelpLink() => "https://docs.npmjs.com/downloading-and-installing-node-js-and-npm";
}
#pragma warning restore ASPIREINTERACTION001
