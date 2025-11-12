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
        IOptions<AzureProvisionerOptions> options,
        DistributedApplicationExecutionContext distributedApplicationExecutionContext)
    {
        _logger = logger;

        // Optionally configured in AppHost appSettings under "Azure" : { "CredentialSource": "AzureCli" }
        var credentialSetting = options.Value.CredentialSource;

        TokenCredential credential = credentialSetting switch
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
            // Use AzureCli as default for publish mode when no explicit credential source is set
            null or "Default" when distributedApplicationExecutionContext.IsPublishMode => new AzureCliCredential(new()
            {
                AdditionallyAllowedTenants = { "*" }
            }),
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
