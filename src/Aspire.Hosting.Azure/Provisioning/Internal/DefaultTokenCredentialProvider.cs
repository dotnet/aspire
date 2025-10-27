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

    public DefaultTokenCredentialProvider(
        ILogger<DefaultTokenCredentialProvider> logger,
        IOptions<AzureProvisionerOptions> options)
    {
        _logger = logger;

        // Optionally configured in AppHost appSettings under "Azure" : { "CredentialSource": "AzureCli" }

        TokenCredential credential = options.Value.CredentialSource switch
        {
            "AzureCli" => new AzureCliCredential(new()
            {
                AdditionallyAllowedTenants = { "*" }
            }),
            "AzurePowerShell" => new AzurePowerShellCredential(new()
            {
                AdditionallyAllowedTenants = { "*" }
            }),
            "VisualStudio" => new VisualStudioCredential(new()
            {
                AdditionallyAllowedTenants = { "*" }
            }),
            "AzureDeveloperCli" => new AzureDeveloperCliCredential(new()
            {
                AdditionallyAllowedTenants = { "*" }
            }),
            "InteractiveBrowser" => new InteractiveBrowserCredential(),
            _ => new DefaultAzureCredential(new DefaultAzureCredentialOptions()
            {
                ExcludeManagedIdentityCredential = true,
                ExcludeWorkloadIdentityCredential = true,
                ExcludeAzurePowerShellCredential = true,
                CredentialProcessTimeout = TimeSpan.FromSeconds(15),
                AdditionallyAllowedTenants = { "*" }
            })
        };

        _credential = credential;
    }

    public TokenCredential TokenCredential => _credential;

    internal void LogCredentialType()
    {
        _logger.LogInformation("Using {credentialType} for provisioning.", _credential.GetType().Name);
    }
}
