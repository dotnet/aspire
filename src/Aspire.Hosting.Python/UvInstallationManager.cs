// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Python;

/// <summary>
/// Validates that the uv command is available on the system.
/// </summary>
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class UvInstallationManager : RequiredCommandValidator
{
    private string? _resolvedCommandPath;

    public UvInstallationManager(
        IInteractionService interactionService,
        ILogger<UvInstallationManager> logger)
        : base(interactionService, logger)
    {
    }

    /// <summary>
    /// Ensures uv is installed/available. This method is safe for concurrent callers;
    /// only one validation will run at a time.
    /// </summary>
    /// <param name="throwOnFailure">Whether to throw an exception if uv is not found. Default is true.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task EnsureInstalledAsync(bool throwOnFailure = true, CancellationToken cancellationToken = default)
    {
        SetThrowOnFailure(throwOnFailure);
        return RunAsync(cancellationToken);
    }

    protected override string GetCommandPath() => "uv";

    protected override Task OnValidatedAsync(string resolvedCommandPath, CancellationToken cancellationToken)
    {
        _resolvedCommandPath = resolvedCommandPath;
        return Task.CompletedTask;
    }

    protected override string? GetHelpLink() => "https://docs.astral.sh/uv/getting-started/installation/";
}
#pragma warning restore ASPIREINTERACTION001
