// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Represents the options for publishing an application.
/// </summary>
public class PublishingOptions
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

    /// <summary>
    /// Gets or sets a value indicating whether the application should be deployed after publishing.
    /// </summary>
    [Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public bool Deploy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether user secrets caching should be disabled during deployment.
    /// When true, deployment configuration will not be read from or saved to user secrets.
    /// </summary>
    [Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public bool NoCache { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether deployment state should be saved to user secrets.
    /// This is set based on user interaction and is only applicable when NoCache is false.
    /// </summary>
    [Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
    public bool? SaveToUserSecrets { get; set; }
}
