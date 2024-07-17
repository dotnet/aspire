// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Hosting.ApplicationModel;

[DebuggerDisplay("Type = {GetType().Name,nq}, SettingsFileOptions = {SettingsFileOptions}")]
internal sealed class SettingsFileAnnotation : IResourceAnnotation
{
    public SettingsFileAnnotation(SettingsFileOptions settingsFileOptions)
    {
        ArgumentNullException.ThrowIfNull(settingsFileOptions);

        SettingsFileOptions = settingsFileOptions;
    }

    public SettingsFileOptions SettingsFileOptions { get; }
}
