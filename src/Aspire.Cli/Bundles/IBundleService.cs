// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Bundles;

/// <summary>
/// Manages extraction of the embedded bundle payload from self-extracting CLI binaries.
/// </summary>
internal interface IBundleService
{
    /// <summary>
    /// Ensures the bundle is extracted for the current CLI binary if it contains an embedded payload.
    /// No-ops if no payload is embedded, or if the layout is already extracted and up to date.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    Task EnsureExtractedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Extracts the bundle payload from the specified binary to the specified directory.
    /// </summary>
    /// <param name="binaryPath">Path to the CLI binary containing the embedded payload.</param>
    /// <param name="destinationPath">Directory to extract into.</param>
    /// <param name="force">If true, re-extract even if the version matches.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the extraction attempt.</returns>
    Task<BundleExtractResult> ExtractAsync(string binaryPath, string destinationPath, bool force = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a bundle extraction attempt.
/// </summary>
internal enum BundleExtractResult
{
    /// <summary>No embedded payload found in the binary.</summary>
    NoPayload,

    /// <summary>Layout already exists and version matches — extraction skipped.</summary>
    AlreadyUpToDate,

    /// <summary>Extraction completed successfully.</summary>
    Extracted,

    /// <summary>Extraction completed but layout validation failed.</summary>
    ExtractionFailed
}
