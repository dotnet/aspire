// Assembly 'Aspire.Azure.AI.OpenAI'

using System;
using System.Runtime.CompilerServices;
using Aspire.Azure.Common;
using Azure.Core;

namespace Aspire.Azure.AI.OpenAI;

public sealed class AzureOpenAISettings : IConnectionStringSettings
{
    public Uri? Endpoint { get; set; }
    public TokenCredential? Credential { get; set; }
    public string? Key { get; set; }
    public bool Tracing { get; set; }
    public AzureOpenAISettings();
}
