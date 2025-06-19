// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

internal class DefaultTokenCredentialProvider : ITokenCredentialProvider
{
    private readonly ILogger<DefaultTokenCredentialProvider> _logger;
    private readonly TokenCredential _credential;

    public DefaultTokenCredentialProvider(ILogger<DefaultTokenCredentialProvider> logger, IOptions<AzureProvisionerOptions> options)
    {
        _logger = logger;

        // Optionally configured in AppHost appSettings under "Azure" : { "CredentialSource": "AzureCli" }
        var credentialSetting = options.Value.CredentialSource;

        TokenCredential credential = credentialSetting switch
        {
            "AzureCli" => new AzureCliCredential(),
            "AzurePowerShell" => new AzurePowerShellCredential(),
            "VisualStudio" => new VisualStudioCredential(),
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

        _credential = credential;
    }

    public TokenCredential TokenCredential => _credential;

    internal void LogCredentialType()
    {
        if (_credential.GetType() == typeof(DefaultAzureCredential))
        {
            _logger.LogInformation(
                "Using DefaultAzureCredential for provisioning. This may not work in all environments. " +
                "See https://aka.ms/azsdk/net/identity/credential-chains#defaultazurecredential-overview for more information.");
        }
        else
        {
            _logger.LogInformation("Using {credentialType} for provisioning.", _credential.GetType().Name);
        }
    }
}
