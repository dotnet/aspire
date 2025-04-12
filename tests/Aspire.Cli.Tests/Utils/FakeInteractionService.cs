// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Spectre.Console;

namespace Aspire.Cli.Tests.Utils;

internal sealed class FakeInteractionServiceOptions
{
    public Func<string, string?, Func<string, ValidationResult>?, CancellationToken, Task<string>> PromptForStringAsyncCallback { get; set; } = (promptText, defaultValue, validator, cancellationToken) => {
        throw new NotImplementedException();
    };
}

internal sealed class FakeInteractionService(FakeInteractionServiceOptions options) : IInteractionService
{
    public void DisplayDashboardUrls((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls)
    {
        throw new NotImplementedException();
    }

    public void DisplayError(string errorMessage)
    {
        throw new NotImplementedException();
    }

    public int DisplayIncompatibleVersionError(AppHostIncompatibleException ex, string appHostHostingSdkVersion)
    {
        throw new NotImplementedException();
    }

    public void DisplayMessage(string emoji, string message)
    {
        throw new NotImplementedException();
    }

    public void DisplaySuccess(string message)
    {
        throw new NotImplementedException();
    }

    public Task<T> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken = default) where T : notnull
    {
        throw new NotImplementedException();
    }

    public Task<string> PromptForStringAsync(string promptText, string? defaultValue = null, Func<string, Spectre.Console.ValidationResult>? validator = null, CancellationToken cancellationToken = default)
    {
        return options.PromptForStringAsyncCallback(promptText, defaultValue, validator, cancellationToken);
    }

    public Task<NuGetPackage> PromptForTemplatesVersionAsync(IEnumerable<NuGetPackage> candidatePackages, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void ShowStatus(string statusText, Action action)
    {
        throw new NotImplementedException();
    }

    public Task<T> ShowStatusAsync<T>(string statusText, Func<Task<T>> action)
    {
        throw new NotImplementedException();
    }
}
