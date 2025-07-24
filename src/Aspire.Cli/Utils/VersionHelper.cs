// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;
using Aspire.Shared;

namespace Aspire.Cli.Utils;

internal static class VersionHelper
{
    public static string GetDefaultTemplateVersion()
    {
        return PackageUpdateHelpers.GetCurrentAssemblyVersion() ?? throw new InvalidOperationException(ErrorStrings.UnableToRetrieveAssemblyVersion);
    }
}
