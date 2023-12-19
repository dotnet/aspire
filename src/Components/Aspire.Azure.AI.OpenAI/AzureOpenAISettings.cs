// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.AI.OpenAI;

/// <summary>
/// The settings relevant to accessing Azure OpenAI or OpenAI.
/// </summary>
public sealed class AzureOpenAISettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the OpenAI service.
    /// </summary>
    /// <remarks>
    /// If <see cref="ConnectionString"/> is set, it overrides <see cref="ServiceUri"/> and <see cref="Credential"/>.
    /// </remarks>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// A <see cref="Uri"/> referencing the Azure OpenAI Endpoint.
    /// This is likely to be similar to "https://{account_name}.openai.azure.com".
    /// </summary>
    /// <remarks>
    /// Used along with <see cref="Credential"/> to establish the connection.
    /// </remarks>
    public Uri? ServiceUri { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool Tracing { get; set; } = true;

    /// <summary>
    /// Gets or sets the credential used to authenticate to the OpenAI service.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets whether to use Azure OpenAI or OpenAI.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true"/>.
    /// </value>
    public bool UseAzureOpenAI { get; set; } = true;

    void IConnectionStringSettings.ParseConnectionString(string? connectionString)
    {
        if (!string.IsNullOrEmpty(connectionString))
        {
            if (Uri.TryCreate(connectionString, UriKind.Absolute, out var uri))
            {
                ServiceUri = uri;
            }
            else
            {
                ConnectionString = connectionString;
            }
        }
    }
}
