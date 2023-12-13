// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;

namespace Aspire.Azure.AI.OpenAI;

/// <summary>
/// The settings relevant to accessing Azure OpenAI.
/// </summary>
public sealed class AzureOpenAISettings
{
    /// <summary>
    /// Gets or sets the connection string used to connect to the OpenAI service.
    /// </summary>
    public string? ConnectionString { get; set; }

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

    /// <summary>
    /// Gets or sets the API key provided in the OpenAI's developer portal.
    /// </summary>
    /// <remarks>
    /// This is required when <see cref="UseAzureOpenAI"/> is set to false.
    /// </remarks>
    public string? OpenAIApiKey { get; set; }

}
