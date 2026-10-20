// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Azure.AppContainers;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Records that an Azure Front Door reaches an Azure Container Apps environment through Private Link. It is added to
/// the Front Door, once per connected environment.
/// </summary>
internal sealed class AzureFrontDoorPrivateLinkAnnotation : IResourceAnnotation
{
    /// <summary>
    /// The sub-resource (group ID) of a Container Apps environment that Front Door connects to.
    /// </summary>
    public const string GroupId = "managedEnvironments";

    // Front Door limits the request message to 140 characters.
    // https://learn.microsoft.com/azure/templates/microsoft.cdn/profiles/origingroups/origins#sharedprivatelinkresourceproperties
    private const int MaxRequestMessageLength = 140;

    public AzureFrontDoorPrivateLinkAnnotation(
        AzureFrontDoorResource frontDoor,
        AzureContainerAppEnvironmentResource environment,
        string? privateLinkLocation)
    {
        Environment = environment;
        PrivateLinkLocation = privateLinkLocation;

        // The environment owner sees this message on the pending connection request, so it names both ends.
        var message = $"Azure Front Door {frontDoor.Name} requests access to Azure Container Apps environment {environment.Name}.";
        RequestMessage = message.Length <= MaxRequestMessageLength ? message : message[..MaxRequestMessageLength];
    }

    /// <summary>
    /// Gets the Container Apps environment the Front Door connects to.
    /// </summary>
    public AzureContainerAppEnvironmentResource Environment { get; }

    /// <summary>
    /// Gets the normalized region of the Front Door private endpoint, or <see langword="null"/> to use the
    /// environment's region.
    /// </summary>
    public string? PrivateLinkLocation { get; }

    /// <summary>
    /// Gets the message that the connection request shows to the environment owner.
    /// </summary>
    public string RequestMessage { get; }
}
