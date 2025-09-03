// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Kusto;

/// <summary>
/// Container image configuration for the Kusto emulator.
/// </summary>
internal static class AzureKustoEmulatorContainerImageTags
{
    /// <summary>
    /// The container registry hosting the Kusto emulator image.
    /// </summary>
    /// <remarks>mcr.microsoft.com</remarks>
    public static string Registry { get; } = "mcr.microsoft.com";

    /// <summary>
    /// The Kusto emulator container image name.
    /// </summary>
    /// <remarks>azuredataexplorer/kustainer-linux</remarks>
    public static string Image { get; } = "azuredataexplorer/kustainer-linux";

    /// <summary>
    /// The tag for the Kusto emulator container image.
    /// </summary>
    /// <remarks>latest</remarks>
    public static string Tag { get; } = "latest";
}
