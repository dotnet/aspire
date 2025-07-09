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
    Task<string> GetCliVersionAsync(string token);
    Task<ValidationResult?> ValidatePromptInputStringAsync(string token, string input);
    Func<string, ValidationResult>? ValidationFunction { get; set; }
}

internal class ExtensionRpcTarget(IConfiguration configuration) : IExtensionRpcTarget
{
    public Func<string, ValidationResult>? ValidationFunction { get; set; }

    [JsonRpcMethod("getCliVersion")]
    public Task<string> GetCliVersionAsync(string token)
    {
        if (!string.Equals(token, configuration[KnownConfigNames.ExtensionToken], StringComparisons.CliInputOrOutput))
        {
            throw new AuthenticationException();
        }

        return Task.FromResult(VersionHelper.GetDefaultTemplateVersion());
    }

    [JsonRpcMethod("validatePromptInputString")]
    public Task<ValidationResult?> ValidatePromptInputStringAsync(string token, string input)
    {
        if (!string.Equals(token, configuration[KnownConfigNames.ExtensionToken], StringComparisons.CliInputOrOutput))
        {
            throw new AuthenticationException();
        }

        return Task.FromResult(ValidationFunction?.Invoke(input));
    }
}
