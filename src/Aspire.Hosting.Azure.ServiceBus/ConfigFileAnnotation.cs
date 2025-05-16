// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.ServiceBus;

/// <summary>
/// Represents an annotation for a custom config file source.
/// </summary>
internal sealed class ConfigFileAnnotation : IResourceAnnotation
{
    public ConfigFileAnnotation(string sourcePath)
    {
        SourcePath = sourcePath ?? throw new ArgumentNullException(nameof(sourcePath));
    }

    public string SourcePath { get; }
}
