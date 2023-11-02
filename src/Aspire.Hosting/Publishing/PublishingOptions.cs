// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents the options for publishing an application.
/// </summary>
public sealed class PublishingOptions
{
    /// <summary>
    /// The name of the publishing configuration section in the appsettings.json file.
    /// </summary>
    public const string Publishing = "Publishing";

    /// <summary>
    /// Gets or sets the name of the publisher responsible for publishing the application.
    /// </summary>
    public string? Publisher { get; set; }
    
    /// <summary>
    /// Gets or sets the path to the directory where the published output will be written.
    /// </summary>
    public string? OutputPath { get; set; }
}
