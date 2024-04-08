// Assembly 'Aspire.Azure.AI.OpenAI'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.AI.OpenAI;

/// <summary>
/// The settings relevant to accessing Azure OpenAI or OpenAI.
/// </summary>
public sealed class AzureOpenAISettings : IConnectionStringSettings
{
    /// <summary>
    /// Gets or sets a <see cref="T:System.Uri" /> referencing the Azure OpenAI endpoint.
    /// This is likely to be similar to "https://{account_name}.openai.azure.com".
    /// </summary>
    /// <remarks>
    /// Leave empty and provide a <see cref="P:Aspire.Azure.AI.OpenAI.AzureOpenAISettings.Key" /> value to use a non-Azure OpenAI inference endpoint.
    /// Used along with <see cref="P:Aspire.Azure.AI.OpenAI.AzureOpenAISettings.Credential" /> or <see cref="P:Aspire.Azure.AI.OpenAI.AzureOpenAISettings.Key" /> to establish the connection.
    /// </remarks>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets the credential used to authenticate to the Azure OpenAI resource.
    /// </summary>
    public TokenCredential? Credential { get; set; }

    /// <summary>
    /// Gets or sets the key to use to authenticate to the Azure OpenAI endpoint.
    /// </summary>
    /// <remarks>
    /// When defined it will use an <see cref="T:Azure.AzureKeyCredential" /> instance instead of <see cref="P:Aspire.Azure.AI.OpenAI.AzureOpenAISettings.Credential" />.
    /// </remarks>
    public string? Key { get; set; }

    /// <summary>
    /// Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.
    /// </summary>
    /// <value>
    /// The default value is <see langword="true" />.
    /// </value>
    public bool Tracing { get; set; }

    public AzureOpenAISettings();
}
