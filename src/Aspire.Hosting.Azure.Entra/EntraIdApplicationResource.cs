// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents a Microsoft Entra ID application registration.
/// </summary>
/// <remarks>
/// <para>
/// Entra ID application registrations define the identity configuration for services
/// in a distributed application. Each application registration includes a tenant ID,
/// client ID, and optionally client credentials and API scopes.
/// </para>
/// <para>
/// This resource injects configuration as environment variables that map to
/// <c>IConfiguration</c> sections compatible with Microsoft.Identity.Web.
/// For example, <c>AzureAd__TenantId</c>, <c>AzureAd__ClientId</c>, etc.
/// </para>
/// <para>
/// The properties align with <c>MicrosoftEntraApplicationOptions</c> from
/// <c>Microsoft.Identity.Abstractions</c>.
/// </para>
/// </remarks>
/// <param name="name">The name of the resource.</param>
/// <param name="configSectionName">
/// The configuration section name used for environment variable prefixes.
/// Defaults to <c>"AzureAd"</c>.
/// </param>
public class EntraIdApplicationResource(string name, string configSectionName = "AzureAd")
    : Resource(name), IResourceWithEnvironment
{
    private const string DefaultInstance = "https://login.microsoftonline.com/";

    /// <summary>
    /// Gets the configuration section name used as the prefix for environment variables.
    /// </summary>
    /// <remarks>
    /// Environment variables are injected as <c>{ConfigSectionName}__{Key}</c>, which
    /// .NET's configuration system maps to <c>{ConfigSectionName}:{Key}</c> in <c>IConfiguration</c>.
    /// </remarks>
    public string ConfigSectionName { get; } = configSectionName;

    // ── Core identity ──────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the Entra ID instance URL.
    /// </summary>
    /// <remarks>
    /// Defaults to <c>https://login.microsoftonline.com/</c>. Override for sovereign clouds
    /// (e.g., <c>https://login.microsoftonline.us/</c> for Azure Government).
    /// </remarks>
    public string Instance { get; set; } = DefaultInstance;

    /// <summary>
    /// Gets or sets the parameter resource for the tenant ID.
    /// </summary>
    public ParameterResource? TenantIdParameter { get; set; }

    /// <summary>
    /// Gets or sets a fixed tenant ID value.
    /// </summary>
    /// <remarks>
    /// When both <see cref="TenantIdParameter"/> and <see cref="TenantId"/> are set,
    /// <see cref="TenantIdParameter"/> takes precedence.
    /// </remarks>
    public string? TenantId { get; set; }

    /// <summary>
    /// Gets or sets the parameter resource for the client ID.
    /// </summary>
    public ParameterResource? ClientIdParameter { get; set; }

    /// <summary>
    /// Gets or sets a fixed client ID value.
    /// </summary>
    /// <remarks>
    /// When both <see cref="ClientIdParameter"/> and <see cref="ClientId"/> are set,
    /// <see cref="ClientIdParameter"/> takes precedence.
    /// </remarks>
    public string? ClientId { get; set; }

    // ── Per-app properties from MicrosoftEntraApplicationOptions ────────

    /// <summary>
    /// Gets or sets the home tenant of the app registration.
    /// </summary>
    /// <remarks>
    /// Useful for multi-tenant apps that acquire tokens on behalf of themselves.
    /// Also provides a direct path to the Azure Portal app registration.
    /// </remarks>
    public string? AppHomeTenantId { get; set; }

    // ── Token acquisition ──────────────────────────────────────────────────

    /// <summary>
    /// Gets the list of client credentials configured for this application.
    /// </summary>
    /// <remarks>
    /// Supports multiple credential types used by Microsoft.Identity.Web:
    /// <list type="bullet">
    /// <item><description><c>ClientSecret</c> — application secret</description></item>
    /// <item><description><c>SignedAssertionFromManagedIdentity</c> — managed identity credential</description></item>
    /// <item><description><c>Certificate</c> — certificate-based credential</description></item>
    /// </list>
    /// </remarks>
    internal List<EntraIdClientCredential> ClientCredentials { get; } = [];

    /// <summary>
    /// Gets or sets the client capabilities (e.g., <c>"cp1"</c> for Continuous Access Evaluation).
    /// </summary>
    internal List<string> ClientCapabilities { get; } = [];

    /// <summary>
    /// Gets or sets the Azure region for optimized token acquisition.
    /// </summary>
    /// <remarks>
    /// Use <c>"TryAutoDetect"</c> to have the app attempt to detect the region automatically.
    /// Per deployment — typically the same for all apps in a deployment.
    /// </remarks>
    public string? AzureRegion { get; set; }

    // ── Web API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Gets the list of audiences accepted by this application registration.
    /// </summary>
    internal List<string> Audiences { get; } = [];

    /// <summary>
    /// Gets or sets whether to allow ACL-based authorization for daemon-to-API calls.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the web API will not require roles or scopes in the token,
    /// allowing client credentials flow callers to be authorized by an Access Control List.
    /// </remarks>
    public bool AllowWebApiToBeAuthorizedByACL { get; set; }

    // ── Diagnostics ────────────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets whether to enable logging of Personally Identifiable Information (PII).
    /// </summary>
    /// <remarks>
    /// The default is <see langword="false"/>. Set to <see langword="true"/> for advanced debugging.
    /// PII logs are never written to default outputs.
    /// </remarks>
    public bool EnablePiiLogging { get; set; }

    // ── Query parameters ───────────────────────────────────────────────────

    /// <summary>
    /// Gets extra query parameters to send to the identity provider.
    /// </summary>
    /// <remarks>
    /// Useful for routing to specific test slices or data centers.
    /// </remarks>
    internal Dictionary<string, string> ExtraQueryParameters { get; } = [];
}

/// <summary>
/// Represents a client credential entry for an Entra ID application registration.
/// </summary>
/// <remarks>
/// <para>
/// Maps to a single entry in the <c>ClientCredentials</c> array in Microsoft.Identity.Web
/// configuration. Each entry describes one credential source, keyed by <see cref="SourceType"/>.
/// </para>
/// <para>
/// The properties on this class mirror those of <c>CredentialDescription</c> from
/// <c>Microsoft.Identity.Abstractions</c>. Only the properties relevant to the chosen
/// <see cref="SourceType"/> need to be set.
/// </para>
/// </remarks>
public sealed class EntraIdClientCredential
{
    /// <summary>
    /// Gets or sets the source type of the credential.
    /// </summary>
    /// <remarks>
    /// Valid values include:
    /// <list type="bullet">
    /// <item><description><c>"ClientSecret"</c> — application secret.</description></item>
    /// <item><description><c>"SignedAssertionFromManagedIdentity"</c> — federated identity credential via managed identity.</description></item>
    /// <item><description><c>"Certificate"</c> — X.509 certificate provided directly.</description></item>
    /// <item><description><c>"KeyVault"</c> — certificate from Azure Key Vault.</description></item>
    /// <item><description><c>"Base64Encoded"</c> — certificate from a base64-encoded value.</description></item>
    /// <item><description><c>"Path"</c> — certificate from a file path.</description></item>
    /// <item><description><c>"StoreWithThumbprint"</c> — certificate from store by thumbprint.</description></item>
    /// <item><description><c>"StoreWithDistinguishedName"</c> — certificate from store by distinguished name.</description></item>
    /// <item><description><c>"SignedAssertionFilePath"</c> — signed assertion from file (e.g., AKS workload identity).</description></item>
    /// <item><description><c>"SignedAssertionFromVault"</c> — signed assertion from Key Vault.</description></item>
    /// </list>
    /// </remarks>
    public required string SourceType { get; set; }

    // ── ClientSecret SourceType ────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the client secret value as a parameter resource.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"ClientSecret"</c>.
    /// </remarks>
    public ParameterResource? ClientSecret { get; set; }

    // ── Managed Identity / FIC ─────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the managed identity client ID for user-assigned managed identity.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"SignedAssertionFromManagedIdentity"</c>.
    /// For system-assigned managed identity, leave this <see langword="null"/>.
    /// </remarks>
    public string? ManagedIdentityClientId { get; set; }

    /// <summary>
    /// Gets or sets the token exchange URL for federated identity credential scenarios.
    /// </summary>
    /// <remarks>
    /// If not specified, defaults to <c>api://AzureADTokenExchange</c>.
    /// </remarks>
    public string? TokenExchangeUrl { get; set; }

    /// <summary>
    /// Gets or sets the token exchange authority URL.
    /// </summary>
    /// <remarks>
    /// Used when the issuer for token exchange differs from the application's authority.
    /// </remarks>
    public string? TokenExchangeAuthority { get; set; }

    // ── KeyVault SourceType ────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the URL of the Azure Key Vault containing the certificate.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"KeyVault"</c> or <c>"SignedAssertionFromVault"</c>.
    /// </remarks>
    public string? KeyVaultUrl { get; set; }

    /// <summary>
    /// Gets or sets the name of the certificate in Azure Key Vault.
    /// </summary>
    /// <remarks>
    /// Used in conjunction with <see cref="KeyVaultUrl"/>.
    /// </remarks>
    public string? KeyVaultCertificateName { get; set; }

    // ── Certificate store ──────────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the certificate store path (e.g., <c>"CurrentUser/My"</c>).
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"StoreWithThumbprint"</c> or
    /// <c>"StoreWithDistinguishedName"</c>.
    /// </remarks>
    public string? CertificateStorePath { get; set; }

    /// <summary>
    /// Gets or sets the certificate thumbprint for store-based lookup.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"StoreWithThumbprint"</c>.
    /// </remarks>
    public string? CertificateThumbprint { get; set; }

    /// <summary>
    /// Gets or sets the certificate distinguished name for store-based lookup.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"StoreWithDistinguishedName"</c>.
    /// </remarks>
    public string? CertificateDistinguishedName { get; set; }

    // ── Certificate from file ──────────────────────────────────────────────

    /// <summary>
    /// Gets or sets the path to a certificate file on disk.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"Path"</c>.
    /// Not recommended for production use.
    /// </remarks>
    public string? CertificateDiskPath { get; set; }

    /// <summary>
    /// Gets or sets the password for the certificate file.
    /// </summary>
    /// <remarks>
    /// Used in conjunction with <see cref="CertificateDiskPath"/> when the certificate is password-protected.
    /// </remarks>
    public string? CertificatePassword { get; set; }

    // ── Base64-encoded certificate ─────────────────────────────────────────

    /// <summary>
    /// Gets or sets the base64-encoded certificate value.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"Base64Encoded"</c>.
    /// Not recommended for production use.
    /// </remarks>
    public string? Base64EncodedValue { get; set; }

    // ── Signed assertion file (AKS workload identity) ──────────────────────

    /// <summary>
    /// Gets or sets the path to a signed assertion file on disk.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="SourceType"/> is <c>"SignedAssertionFilePath"</c>.
    /// Typically used with Azure Kubernetes Service workload identity federation.
    /// If not provided, the <c>AZURE_FEDERATED_TOKEN_FILE</c> environment variable is used.
    /// </remarks>
    public string? SignedAssertionFileDiskPath { get; set; }
}
