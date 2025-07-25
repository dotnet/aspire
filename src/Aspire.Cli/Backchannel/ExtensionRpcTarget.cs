// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Authentication;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Spectre.Console;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal interface IExtensionRpcTarget
{
    Func<string, ValidationResult>? ValidationFunction { get; set; }

    [JsonRpcMethod("getCliVersion")]
    Task<string> GetCliVersionAsync(string token);

    [JsonRpcMethod("validatePromptInputString")]
    Task<ValidationResult?> ValidatePromptInputStringAsync(string token, string input);

    [JsonRpcMethod("stopCli")]
    Task StopCliAsync(string token);
}

internal class ExtensionRpcTarget(IConfiguration configuration) : IExtensionRpcTarget
{
    public Func<string, ValidationResult>? ValidationFunction { get; set; }

    public Task<string> GetCliVersionAsync(string token)
    {
        if (!string.Equals(token, configuration[KnownConfigNames.ExtensionToken], StringComparisons.CliInputOrOutput))
        {
            throw new AuthenticationException();
        }

        return Task.FromResult(VersionHelper.GetDefaultTemplateVersion());
    }

    public Task<ValidationResult?> ValidatePromptInputStringAsync(string token, string input)
    {
        if (!string.Equals(token, configuration[KnownConfigNames.ExtensionToken], StringComparisons.CliInputOrOutput))
        {
            throw new AuthenticationException();
        }

        return Task.FromResult(ValidationFunction?.Invoke(input));
    }

    public Task StopCliAsync(string token)
    {
        if (!string.Equals(token, configuration[KnownConfigNames.ExtensionToken], StringComparisons.CliInputOrOutput))
        {
            throw new AuthenticationException();
        }

        Environment.Exit(ExitCodeConstants.Success);
        return Task.CompletedTask;
    }
}
