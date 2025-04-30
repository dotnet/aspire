// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning;

internal class TokenCredentialHolder
{
    private readonly ILogger<TokenCredentialHolder> _logger;

    public TokenCredentialHolder(ILogger<TokenCredentialHolder> logger, IOptions<AzureProvisionerOptions> options)
    {
        _logger = logger;

        // Optionally configured in AppHost appSettings under "Azure" : { "CredentialSource": "AzureCli" }
        var credentialSetting = options.Value.CredentialSource;

        TokenCredential credential = credentialSetting switch
        {
            "AzureCli" => new AzureCliCredential(),
            "AzurePowerShell" => new AzurePowerShellCredential(),
            "VisualStudio" => new VisualStudioCredential(),
            "VisualStudioCode" => new VisualStudioCodeCredential(),
            "AzureDeveloperCli" => new AzureDeveloperCliCredential(),
            "InteractiveBrowser" => new InteractiveBrowserCredential(),
            _ => new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ExcludeManagedIdentityCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                ExcludeAzurePowerShellCredential = true,
                CredentialProcessTimeout = TimeSpan.FromSeconds(15)
            })
        };

        Credential = credential;
    }

    public TokenCredential Credential { get; }

    internal void LogCredentialType()
    {
        if (Credential.GetType() == typeof(DefaultAzureCredential))
        {
            _logger.LogInformation(
                "Using DefaultAzureCredential for provisioning. This may not work in all environments. " +
                "See https://aka.ms/azsdk/net/identity/credential-chains#defaultazurecredential-overview for more information.");
        }
        else
        {
            _logger.LogInformation("Using {credentialType} for provisioning.", Credential.GetType().Name);
        }
    }
}
