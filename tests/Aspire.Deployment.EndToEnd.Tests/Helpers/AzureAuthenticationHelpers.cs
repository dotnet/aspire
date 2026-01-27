// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.Identity;

namespace Aspire.Deployment.EndToEnd.Tests.Helpers;

/// <summary>
/// Helper methods for Azure authentication in deployment tests.
/// Supports both OIDC (Workload Identity Federation) in CI and Azure CLI locally.
/// </summary>
internal static class AzureAuthenticationHelpers
{
    private const string SubscriptionEnvVar = "ASPIRE_DEPLOYMENT_TEST_SUBSCRIPTION";
    private const string ResourceGroupPrefixEnvVar = "ASPIRE_DEPLOYMENT_TEST_RG_PREFIX";
    private const string DefaultResourceGroupPrefix = "aspire-e2e";

    /// <summary>
    /// Gets whether Azure authentication is available.
    /// Returns true if we have either OIDC credentials (CI) or Azure CLI credentials (local).
    /// </summary>
    internal static bool IsAzureAuthAvailable()
    {
        try
        {
            var credential = GetAzureCredential();
            // Try to get a token to validate credentials work
            var context = new TokenRequestContext(["https://management.azure.com/.default"]);
            credential.GetToken(context, CancellationToken.None);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Gets the Azure credential to use for authentication.
    /// In CI (GitHub Actions), uses DefaultAzureCredential which will pick up OIDC.
    /// Locally, falls back to Azure CLI credentials.
    /// </summary>
    internal static TokenCredential GetAzureCredential()
    {
        // DefaultAzureCredential tries multiple authentication methods in order:
        // 1. Environment variables (AZURE_CLIENT_ID, AZURE_TENANT_ID for OIDC in CI)
        // 2. Managed Identity
        // 3. Azure CLI
        // 4. Azure PowerShell
        // 5. etc.
        return new DefaultAzureCredential();
    }

    /// <summary>
    /// Gets the subscription ID from environment variable.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when subscription ID is not configured.</exception>
    internal static string GetSubscriptionId()
    {
        var subscriptionId = Environment.GetEnvironmentVariable(SubscriptionEnvVar);

        if (string.IsNullOrEmpty(subscriptionId))
        {
            // Also check AZURE_SUBSCRIPTION_ID as fallback
            subscriptionId = Environment.GetEnvironmentVariable("AZURE_SUBSCRIPTION_ID");
        }

        if (string.IsNullOrEmpty(subscriptionId))
        {
            throw new InvalidOperationException(
                $"Azure subscription ID not configured. Set the {SubscriptionEnvVar} or AZURE_SUBSCRIPTION_ID environment variable.");
        }

        return subscriptionId;
    }

    /// <summary>
    /// Gets the subscription ID if available, or null if not configured.
    /// </summary>
    internal static string? TryGetSubscriptionId()
    {
        try
        {
            return GetSubscriptionId();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Gets the resource group prefix for naming test resources.
    /// </summary>
    internal static string GetResourceGroupPrefix()
    {
        var prefix = Environment.GetEnvironmentVariable(ResourceGroupPrefixEnvVar);
        return string.IsNullOrEmpty(prefix) ? DefaultResourceGroupPrefix : prefix;
    }

    /// <summary>
    /// Generates a unique resource group name for a test run.
    /// Format: {prefix}-{date}-{runId or random}
    /// </summary>
    internal static string GenerateResourceGroupName(string? testName = null)
    {
        var prefix = GetResourceGroupPrefix();
        var date = DateTime.UtcNow.ToString("yyyyMMdd");

        // Use GitHub run ID if available, otherwise generate random suffix
        var runId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
        var suffix = !string.IsNullOrEmpty(runId)
            ? runId[..Math.Min(8, runId.Length)]
            : Guid.NewGuid().ToString("N")[..8];

        if (!string.IsNullOrEmpty(testName))
        {
            // Sanitize test name for Azure resource naming (lowercase, alphanumeric, hyphens)
            var sanitizedName = new string(testName
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray())
                .Trim('-');

            // Truncate if too long (Azure RG names max 90 chars)
            if (sanitizedName.Length > 20)
            {
                sanitizedName = sanitizedName[..20];
            }

            return $"{prefix}-{sanitizedName}-{date}-{suffix}";
        }

        return $"{prefix}-{date}-{suffix}";
    }

    /// <summary>
    /// Gets the Azure tenant ID if configured.
    /// </summary>
    internal static string? GetTenantId()
    {
        return Environment.GetEnvironmentVariable("AZURE_TENANT_ID");
    }

    /// <summary>
    /// Gets the Azure client ID for OIDC authentication if configured.
    /// </summary>
    internal static string? GetClientId()
    {
        return Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
    }

    /// <summary>
    /// Checks if OIDC credentials are configured (for CI environment).
    /// </summary>
    internal static bool IsOidcConfigured()
    {
        return !string.IsNullOrEmpty(GetClientId()) && !string.IsNullOrEmpty(GetTenantId());
    }
}
