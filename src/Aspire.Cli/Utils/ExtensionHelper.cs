// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Cli.Utils;

internal class ExtensionHelper
{
    public const string DevKitCapability = "devkit";
    public const string CSharpCapability = "csharp";

    public static bool IsExtensionHost(
        IServiceProvider serviceProvider,
        [NotNullWhen(true)] out IExtensionInteractionService? interactionService,
        [NotNullWhen(true)] out IExtensionBackchannel? extensionBackchannel)
    {
        if (serviceProvider.GetRequiredService<IInteractionService>() is IExtensionInteractionService extensionInteractionService)
        {
            interactionService = extensionInteractionService;
            extensionBackchannel = serviceProvider.GetRequiredService<IExtensionBackchannel>();
            return true;
        }

        interactionService = null;
        extensionBackchannel = null;
        return false;
    }
}
