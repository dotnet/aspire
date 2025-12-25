// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.CodeGeneration;

/// <summary>
/// Factory for creating extension method providers based on the current context.
/// </summary>
internal sealed class ExtensionMethodProviderFactory
{
    private readonly string? _localRepoRoot;

    /// <summary>
    /// Configuration key for the local Aspire repo root path.
    /// </summary>
    public const string LocalRepoRootConfigKey = "ASPIRE_REPO_ROOT";

    public ExtensionMethodProviderFactory(IConfiguration configuration)
    {
        _localRepoRoot = configuration[LocalRepoRootConfigKey];
    }

    /// <summary>
    /// Creates an extension method provider.
    /// </summary>
    /// <returns>
    /// An <see cref="InferredExtensionMethodProvider"/> that infers methods from package naming conventions.
    /// </returns>
    /// <remarks>
    /// TODO: Add assembly scanning support for local development when we have an AOT-compatible solution.
    /// This would allow reading PolyglotMethodNameAttribute from actual assemblies.
    /// </remarks>
    public IExtensionMethodProvider CreateProvider()
    {
        // TODO: When we have an AOT-compatible solution, use _localRepoRoot
        // to enable assembly scanning for PolyglotMethodNameAttribute
        _ = _localRepoRoot;
        return new InferredExtensionMethodProvider();
    }

    /// <summary>
    /// Gets whether local development mode is enabled.
    /// </summary>
    public bool IsLocalDevelopment => !string.IsNullOrEmpty(_localRepoRoot);

    /// <summary>
    /// Gets the local repository root path, if set.
    /// </summary>
    public string? LocalRepoRoot => _localRepoRoot;
}
