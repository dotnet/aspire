// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Utils;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal interface IExtensionRpcTarget
{
    Task<string> GetCliVersionAsync();
}

internal class ExtensionRpcTarget : IExtensionRpcTarget
{
    [JsonRpcMethod("getCliVersion")]
    public Task<string> GetCliVersionAsync()
    {
        return Task.FromResult(VersionHelper.GetDefaultTemplateVersion());
    }
}
