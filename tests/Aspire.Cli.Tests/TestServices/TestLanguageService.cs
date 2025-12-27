// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Projects;

namespace Aspire.Cli.Tests.TestServices;

internal sealed class TestLanguageService : ILanguageService
{
    public Func<CancellationToken, Task<AppHostLanguage?>>? GetConfiguredLanguageAsyncCallback { get; set; }
    public Func<AppHostLanguage, bool, CancellationToken, Task>? SetLanguageAsyncCallback { get; set; }
    public Func<CancellationToken, Task<AppHostLanguage>>? PromptForLanguageAsyncCallback { get; set; }
    public Func<string?, bool, CancellationToken, Task<AppHostLanguage>>? GetOrPromptForLanguageAsyncCallback { get; set; }

    /// <summary>
    /// The default language to return when no callback is set.
    /// </summary>
    public AppHostLanguage DefaultLanguage { get; set; } = AppHostLanguage.CSharp;

    public Task<AppHostLanguage?> GetConfiguredLanguageAsync(CancellationToken cancellationToken = default)
    {
        return GetConfiguredLanguageAsyncCallback is not null
            ? GetConfiguredLanguageAsyncCallback(cancellationToken)
            : Task.FromResult<AppHostLanguage?>(DefaultLanguage);
    }

    public Task SetLanguageAsync(AppHostLanguage language, bool isGlobal = false, CancellationToken cancellationToken = default)
    {
        return SetLanguageAsyncCallback is not null
            ? SetLanguageAsyncCallback(language, isGlobal, cancellationToken)
            : Task.CompletedTask;
    }

    public Task<AppHostLanguage> PromptForLanguageAsync(CancellationToken cancellationToken = default)
    {
        return PromptForLanguageAsyncCallback is not null
            ? PromptForLanguageAsyncCallback(cancellationToken)
            : Task.FromResult(DefaultLanguage);
    }

    public Task<AppHostLanguage> GetOrPromptForLanguageAsync(string? explicitLanguage = null, bool saveSelection = true, CancellationToken cancellationToken = default)
    {
        return GetOrPromptForLanguageAsyncCallback is not null
            ? GetOrPromptForLanguageAsyncCallback(explicitLanguage, saveSelection, cancellationToken)
            : Task.FromResult(DefaultLanguage);
    }
}
