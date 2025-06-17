// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Resources;

namespace Aspire.Cli.Utils;

internal static class VersionHelper
{
    public static string GetDefaultTemplateVersion()
    {
        // Write some code that gets the informational assembly version of the current assembly and returns it as a string.
        var assembly = typeof(VersionHelper).Assembly;
        var informationalVersion = assembly
            .GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), false)
            .OfType<System.Reflection.AssemblyInformationalVersionAttribute>()
            .FirstOrDefault()?.InformationalVersion;

        return informationalVersion ?? throw new InvalidOperationException(ErrorStrings.UnableToRetrieveAssemblyVersion);
    }
}
