// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Utils;

/// <summary>
/// Represents the result of a prerequisite check.
/// </summary>
internal sealed class PrerequisiteCheckResult
{
    /// <summary>
    /// Gets the category of the check (e.g., "sdk", "container", "environment").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Gets the name of the specific check (e.g., "dotnet-10", "daemon-running").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the status of the check.
    /// </summary>
    public PrerequisiteCheckStatus Status { get; init; }

    /// <summary>
    /// Gets the human-readable message describing the check result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional fix suggestion for addressing the issue.
    /// </summary>
    public string? Fix { get; init; }

    /// <summary>
    /// Gets the optional documentation link for more information.
    /// </summary>
    public string? Link { get; init; }

    /// <summary>
    /// Gets optional additional details about the check.
    /// </summary>
    public string? Details { get; init; }
}

/// <summary>
/// Represents the status of a prerequisite check.
/// </summary>
internal enum PrerequisiteCheckStatus
{
    /// <summary>
    /// The check passed successfully.
    /// </summary>
    Pass,

    /// <summary>
    /// The check completed with a warning (non-blocking).
    /// </summary>
    Warning,

    /// <summary>
    /// The check failed (blocking issue).
    /// </summary>
    Fail
}
