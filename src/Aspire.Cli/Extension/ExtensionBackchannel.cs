// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Backchannel;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Extension;

internal interface IExtensionBackchannel : IBackchannel;

internal sealed class ExtensionBackchannel(ILogger<ExtensionBackchannel> logger, CliRpcTarget target) : BaseBackchannel<ExtensionBackchannel>(logger, target), IExtensionBackchannel
{
    public override string BaselineCapability => "baseline";

    public override void CheckCapabilities(string[] capabilities)
    {
        if (!capabilities.Any(s => s == BaselineCapability))
        {
            throw new ExtensionIncompatibleException(
                $"The Aspire extension is incompatible with the CLI. The extension must be updated to a version that supports the {BaselineCapability} capability.",
                BaselineCapability
            );
        }
    }
}
