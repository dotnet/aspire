// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Default values for the Kusto emulator container.
/// </summary>
internal static class AzureKustoEmulatorContainerDefaults
{
    /// <summary>
    /// The default target port for the Kusto emulator container query endpoint.
    /// Based on Azure Data Explorer emulator documentation, it typically uses port 8080.
    /// </summary>
    public const int DefaultTargetPort = 8080;
}
