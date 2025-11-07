// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Python;

/// <summary>
/// Validates that the Python executable is available on the system.
/// </summary>
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class PythonInstallationManager : RequiredCommandValidator
{
    private string? _resolvedCommandPath;

    public PythonInstallationManager(
        IInteractionService interactionService,
        ILogger<PythonInstallationManager> logger)
        : base(interactionService, logger)
    {
    }

    /// <summary>
    /// Gets the resolved full path to the Python executable after a successful validation, otherwise <c>null</c>.
    /// </summary>
    public string? ResolvedCommandPath => _resolvedCommandPath;

    /// <summary>
    /// Gets a value indicating whether Python was found (after calling <see cref="EnsureInstalledAsync"/>).
    /// </summary>
    public bool IsInstalled => _resolvedCommandPath is not null;

    /// <summary>
    /// Ensures Python is installed/available. This method is safe for concurrent callers;
    /// only one validation will run at a time.
    /// </summary>
    public Task EnsureInstalledAsync(CancellationToken cancellationToken = default) => RunAsync(cancellationToken);

    protected override string GetCommandPath()
    {
        // Try common Python command names based on platform
        // On Windows: python, py
        // On Linux/macOS: python3, python
        if (OperatingSystem.IsWindows())
        {
            // Try 'python' first, then 'py' (Python launcher)
            var pythonPath = ResolveCommand("python");
            if (pythonPath is not null)
            {
                return "python";
            }
            return "py";
        }
        else
        {
            // Try 'python3' first (more specific), then 'python'
            var python3Path = ResolveCommand("python3");
            if (python3Path is not null)
            {
                return "python3";
            }
            return "python";
        }
    }

    protected override Task OnValidatedAsync(string resolvedCommandPath, CancellationToken cancellationToken)
    {
        _resolvedCommandPath = resolvedCommandPath;
        return Task.CompletedTask;
    }

    protected override string? GetHelpLink() => "https://www.python.org/downloads/";
}
#pragma warning restore ASPIREINTERACTION001
