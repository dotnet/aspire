// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.EntraId.Provisioning;

/// <summary>
/// Shared identifiers for Entra provisioning outputs and Microsoft Graph resource types.
/// </summary>
internal static class EntraIdBicepConstants
{
    /// <summary>The application client ID output name.</summary>
    internal const string ClientIdOutputName = "clientId";

    /// <summary>The home tenant ID output name.</summary>
    internal const string TenantIdOutputName = "tenantId";

    /// <summary>The application object ID output name.</summary>
    internal const string ObjectIdOutputName = "objectId";

    /// <summary>The service principal object ID output name.</summary>
    internal const string ServicePrincipalIdOutputName = "servicePrincipalId";

    /// <summary>The application unique name output name.</summary>
    internal const string UniqueNameOutputName = "uniqueName";

    /// <summary>The Microsoft Graph application resource type.</summary>
    internal const string ApplicationResourceType = "Microsoft.Graph/applications";

    /// <summary>The Microsoft Graph service principal resource type.</summary>
    internal const string ServicePrincipalResourceType = "Microsoft.Graph/servicePrincipals";

    /// <summary>The Microsoft Graph resource API version.</summary>
    internal const string GraphApiVersion = "v1.0";
}
