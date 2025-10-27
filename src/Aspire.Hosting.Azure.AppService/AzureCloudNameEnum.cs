// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.AppService;

/// <summary>
/// Represents the names of the Azure clouds.
/// </summary>
/// <remarks>Azure provides multiple clouds to meet the needs of different regions and compliance
/// requirements. This enumeration is used to specify the target Azure cloud.</remarks>
public enum AzureCloudName
{
    /// <summary>
    /// Represents the Azure public cloud.
    /// </summary>
    AzurePublic,
    /// <summary>
    /// Represents the Azure US Government cloud.
    /// </summary>
    AzureUSGovernment,
    /// <summary>
    /// Represents the Azure China cloud.
    /// </summary>
    AzureChina,
    /// <summary>
    /// Represents the Azure Germany cloud.
    /// </summary>
    AzureGermany,
}
