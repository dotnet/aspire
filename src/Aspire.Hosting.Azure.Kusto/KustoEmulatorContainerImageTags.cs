// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Container image configuration for the Kusto emulator.
/// </summary>
internal static class KustoEmulatorContainerImageTags
{
    /// <summary>
    /// The container registry hosting the Kusto emulator image.
    /// </summary>
    public static string Registry { get; } = "mcr.microsoft.com";

    /// <summary>
    /// The Kusto emulator container image name.
    /// </summary>
    public static string Image { get; } = "azuredataexplorer/kustainer-linux";

    /// <summary>
    /// The tag for the Kusto emulator container image.
    /// </summary>
    public static string Tag { get; } = "latest";
}
