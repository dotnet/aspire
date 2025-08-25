// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Aspire.Cli.Backchannel;
using Aspire.Cli.Interaction;

namespace Aspire.Cli.Utils;

internal class ExtensionHelper
{
    public static bool IsExtensionHost(
        IInteractionService interactionService,
        [NotNullWhen(true)] out IExtensionInteractionService? extensionInteractionService,
        [NotNullWhen(true)] out IExtensionBackchannel? extensionBackchannel)
    {
        if (interactionService is IExtensionInteractionService eis)
        {
            extensionInteractionService = eis;
            extensionBackchannel = eis.Backchannel;
            return true;
        }

        extensionInteractionService = null;
        extensionBackchannel = null;
        return false;
    }
}

internal static class KnownCapabilities
{
    public const string DevKit = "devkit";
    public const string Project = "project";
}
