// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure;
using Aspire.Hosting.Azure.Extensions;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for adding the Azure Front Door resources to the application model.
/// </summary>
public static class AzureFrontDoorConfigurationExtensions
{
    /// <summary>
    /// Adds an Azure Front Door resource to the application model.
    /// </summary>
    /// <param name="builder">The <see cref="IDistributedApplicationBuilder"/>.</param>
    /// <param name="name">The name of the resource. This name will be used as the connection string name when referenced in a dependency.</param>
    /// <param name="options">Configurable options used to specify deployment properties of the deployed resource.</param>
    /// <returns>A reference to the <see cref="IResourceBuilder{T}"/>.</returns>
    public static IResourceBuilder<AzureFrontDoorResource> AddAzureFrontDoor(
        this IDistributedApplicationBuilder builder, string name, AzureFrontDoorOptions options)
    {
        var resource = new AzureFrontDoorResource(name);
        return builder.AddResource(resource)
            .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
            .WithParameter(AzureBicepResource.KnownParameters.PrincipalName)
            .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
            .WithParameter("domainName", options.DomainName)
            .WithParameter("frontDoorProfileName", resource.CreateBicepResourceName())
            .WithParameter("skuName", options.Sku.GetValueFromEnumMember())
            .WithParameter("healthProbePath", options.HealthProbePath)
            .WithParameter("queryStringCachingBehavior", options.QueryStringCachingBehavior.GetValueFromEnumMember())
            .WithParameter("queryParameters", string.Join(",", options.QueryParameters))
            .WithParameter("forwardingProtocol", options.ForwardingProtocol.GetValueFromEnumMember())
            .WithParameter("supportedProtocols", options.SupportedProtocols.ToList().Select(protocol => protocol.GetValueFromEnumMember()))
            .WithParameter("contentTypesToCompress", options.ContentTypesToCompress)
            .WithParameter("allowSessionAffinity", options.AllowSessionAffinity)
            .WithParameter("httpsRedirect", options.SupportHttpsRedirect)
            .WithParameter("createWwwSubdomainForCustomDomain", options.CreateWwwSubdomainForDomain)
            .WithParameter("dnsZoneName", options.DnsZoneName)
            .WithParameter("dnsRecordTimeToLiveInSeconds", options.DnsRecordTtl)
            .WithParameter("originContainerAppName", options.OriginContainerAppName)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
}

/// <summary>
/// Configuration options necessary for creating an Azure Front Door resource.
/// </summary>
public sealed class AzureFrontDoorOptions
{
    /// <summary>
    /// Constructor used to create an instance of <see cref="AzureFrontDoorOptions"/>.
    /// </summary>
    /// <param name="domainName">The domain name to provision the Front Door endpoint and route for.</param>
    /// <param name="originContainerAppName">The name of the Container App that Front Door should be configured to point to as the origin.</param>
    /// <param name="dnsZoneName">The name of the DNS zone to configure alongside the Front Door instance for custom domain certificate issuance and ingest.</param>
    public AzureFrontDoorOptions(string domainName, string originContainerAppName, string dnsZoneName)
    {
        DomainName = domainName;
        OriginContainerAppName = originContainerAppName;
        DnsZoneName = dnsZoneName;
    }

    /// <summary>
    /// The domain name to provision the Front Door endpoint and route for.
    /// </summary>
    public string DomainName { get; init; }

    /// <summary>
    /// Flag indicating whether a "www." subdomain should be created in addition to the domain for <see cref="DomainName"/>.
    /// </summary>
    public bool CreateWwwSubdomainForDomain { get; init; }

    /// <summary>
    /// The name of the DNS zone to configure alongside the Front Door instance.
    /// </summary>
    public string DnsZoneName { get; init; }

    /// <summary>
    /// The name of the container 
    /// </summary>
    public string OriginContainerAppName { get; init; }

    /// <summary>
    /// The path used for the health probe.
    /// </summary>
    public string HealthProbePath { get; init; } = "/health";

    /// <summary>
    /// Specifies how Front Door caches requests that include query strings.
    /// </summary>
    public FrontDoorQueryStringCachingBehavior QueryStringCachingBehavior { get; init; } =
        FrontDoorQueryStringCachingBehavior.IgnoreQueryString;

    /// <summary>
    /// The query parameters to include or exclude based on the value of <see cref="QueryStringCachingBehavior"/>.
    /// </summary>
    public List<string> QueryParameters { get; init; } = [];

    /// <summary>
    /// The protocol the rule will use when forwarding traffic to backends.
    /// </summary>
    public FrontDoorForwardingProtocol ForwardingProtocol = FrontDoorForwardingProtocol.MatchRequest;

    /// <summary>
    /// The list of supported protocols for the Front Door route.
    /// </summary>
    public FrontDoorSupportedProtocol SupportedProtocols =
        FrontDoorSupportedProtocol.Http | FrontDoorSupportedProtocol.Https;
    
    /// <summary>
    /// Indicates whether the Front Door route should support HTTPS redirect.
    /// </summary>
    public bool SupportHttpsRedirect { get; init; } = true;

    /// <summary>
    /// The list of content MIME types to compress, if any.
    /// </summary>
    public List<string> ContentTypesToCompress { get; init; } = [];

    /// <summary>
    /// The Front Door SKU to spin up the resource for.
    /// </summary>
    public FrontDoorSku Sku { get; init; } = FrontDoorSku.Standard;

    /// <summary>
    /// Indicates whether the Front Door origin group should support session affinity.
    /// </summary>
    public bool AllowSessionAffinity { get; init; }

    /// <summary>
    /// The DNS record time-to-live value as measured in seconds.
    /// </summary>
    public int DnsRecordTtl { get; init; } = 3600;
}

/// <summary>
/// Reflects the various Front Door SKUs.
/// </summary>
public enum FrontDoorSku
{
    /// <summary>
    /// Reflects the Premium Front Door SKU.
    /// </summary>
    [EnumMember(Value="Premium_AzureFrontDoor")]
    Premium,
    /// <summary>
    /// Reflects the Standard Front Door SKU.
    /// </summary>
    [EnumMember(Value="Standard_AzureFrontDoor")]
    Standard
}

/// <summary>
/// Specifies the protocol that will be used when forwarding traffic to backends.
/// </summary>
public enum FrontDoorForwardingProtocol
{
    /// <summary>
    /// Configures Front Door to only forward HTTP requests.
    /// </summary>
    [EnumMember(Value="HttpOnly")]
    HttpOnly,
    /// <summary>
    /// Configures Front Door to only forward HTTPS requests.
    /// </summary>
    [EnumMember(Value="HttpsOnly")]
    HttpsOnly,
    /// <summary>
    /// Configures Front Door to forward requests based on whatever protocol they're already using.
    /// </summary>
    [EnumMember(Value="MatchRequest")]
    MatchRequest
}

/// <summary>
/// Specifies the list of supported protocols for this route.
/// </summary>
[Flags]
public enum FrontDoorSupportedProtocol
{
    /// <summary>
    /// Identifies the HTTP protocol
    /// </summary>
    [EnumMember(Value="Http")]
    Http = 0,
    /// <summary>
    /// Identifies the HTTPS protocol.
    /// </summary>
    [EnumMember(Value="Https")]
    Https = 1
}

/// <summary>
/// Specifies the caching behavior of a Front Door resource.
/// </summary>
public enum FrontDoorQueryStringCachingBehavior
{
    /// <summary>
    /// Indicates that the query string as a whole should be used in caching.
    /// </summary>
    [EnumMember(Value = "UseQueryString")]
    UseQueryString,
    /// <summary>
    /// Indicates that the query string as a whole should be ignored.
    /// </summary>
    [EnumMember(Value = "IgnoreQueryString")]
    IgnoreQueryString,
    /// <summary>
    /// Indicates that the query strings specified should be ignored.
    /// </summary>
    [EnumMember(Value = "IgnoreSpecifiedQueryStrings")]
    IgnoreSpecifiedQueryStrings,
    /// <summary>
    /// Indicates that the query strings specified should be cached.
    /// </summary>
    [EnumMember(Value = "IncludeSpecifiedQueryStrings")]
    IncludeSpecifiedQueryStrings
};
