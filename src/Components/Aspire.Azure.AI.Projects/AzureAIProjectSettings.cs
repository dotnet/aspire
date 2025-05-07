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
    private string? _connectionString;
    private bool? _disableTracing;

    /// <summary>
    /// Gets or sets the connection string used to connect to the table service account. 
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="Endpoint"/>, <see cref="SubscriptionId"/>, <see cref="ResourceGroupName"/>, <see cref="ProjectName"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString
    {
        get
        {
            if (_connectionString is null && Endpoint is not null)
            {
                _connectionString = $"{Endpoint.Host};{SubscriptionId};{ResourceGroupName};{ProjectName}";
            }

            return _connectionString;
        }
        set => _connectionString = value;
    }

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
    /// <remarks>
    /// ServiceBus ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    /// <value>
    /// The default value is <see langword="false"/>.
    /// </value>
    public bool DisableTracing
    {
        get { return _disableTracing ??= !GetTracingDefaultValue(); }
        set { _disableTracing = value; }
    }

    // Defaults DisableTracing to false if the experimental switch is set
    // TODO: remove this when ActivitySource support is no longer experimental
    private static bool GetTracingDefaultValue()
    {
        if (AppContext.TryGetSwitch("Azure.Experimental.EnableActivitySource", out var enabled))
        {
            return enabled;
        }

        var envVar = Environment.GetEnvironmentVariable("AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE");
        if (envVar is not null && (envVar.Equals("true", StringComparison.OrdinalIgnoreCase) || envVar.Equals("1")))
        {
            return true;
        }

        return false;
    }

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
