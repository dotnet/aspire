// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.AI.Projects;
using Azure.Core;

namespace Aspire.Azure.AI.Projects;

/// <summary>
/// The settings for configuring an <see cref="AIProjectClient"/>.
/// </summary>
public sealed class AzureAIProjectSettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the table service account. 
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="Endpoint"/>, <see cref="SubscriptionId"/>, <see cref="ResourceGroupName"/>, <see cref="ProjectName"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// The Endpoint of the resource in Azure.
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="SubscriptionId"/>, <see cref="ResourceGroupName"/>, <see cref="ProjectName"/> and <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// The ID of the Azure subscription for the resource.
    /// </summary>
    public string? SubscriptionId { get; set; }

    /// <summary>
    /// The name of the resource group the AI Foundry is deployed to.
    /// </summary>
    public string? ResourceGroupName { get; set; }

    /// <summary>
    /// The name of the AI Foundry project to use.
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to Azure AI Foundry.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is disabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing { get; set; }

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        ArgumentException.ThrowIfNullOrEmpty(connectionString, nameof(connectionString));

        // Split the connection string by ';'
        var parts = connectionString.Split(';');
        if (parts.Length != 4)
        {
            throw new ArgumentException("Invalid connection string format. Expected format: <endpoint>;<subscription_id>;<resource_group_name>;<project_name>", nameof(connectionString));
        }

        Endpoint = parts[0].StartsWith("https") ? new(parts[0]) : new("https://" + parts[0]);
        SubscriptionId = parts[1];
        ResourceGroupName = parts[2];
        ProjectName = parts[3];
    }
}
