// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREAZURE003 // Type is for evaluation purposes only and is subject to change or removal in future updates.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;
using Azure.Provisioning.Cdn;
using Azure.Provisioning.Expressions;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Converts the origins of one Front Door template that run in a compute environment connected with
/// <see cref="AzureFrontDoorPrivateLinkExtensions.WithAzureFrontDoorPrivateLink"/> into Private Link origins.
/// </summary>
internal sealed class AzureFrontDoorPrivateLinkConfigurator(AzureResourceInfrastructure infrastructure, AzureFrontDoorResource frontDoor)
{
    // The Bicep values that identify a connected environment. Every origin in the environment shares them, and Front
    // Door shares one private endpoint across those origins.
    private readonly Dictionary<AzureFrontDoorPrivateLinkAnnotation, (ProvisioningParameter EnvironmentId, BicepValue<string> Location)> _targets = [];

    /// <summary>
    /// Converts <paramref name="origin"/> into a Private Link origin when it runs in a connected environment.
    /// </summary>
    /// <returns><see langword="true"/> when the origin uses Private Link; otherwise <see langword="false"/>.</returns>
    public bool TryConfigureOrigin(
        IComputeEnvironmentResource computeEnvironment,
        string originName,
        FrontDoorOriginGroup originGroup,
        FrontDoorOrigin origin)
    {
        if (computeEnvironment is not IAzureCdnPrivateLinkTarget environment ||
            !environment.Annotations.OfType<PrivateEndpointTargetAnnotation>().Any(annotation => annotation.PrivateEndpointResource is AzureFrontDoorResource))
        {
            return false;
        }

        var connection = frontDoor.Annotations
            .OfType<AzureFrontDoorPrivateLinkAnnotation>()
            .FirstOrDefault(annotation => ReferenceEquals(annotation.Environment, environment));
        if (connection is null)
        {
            // The environment refuses public traffic, so a Front Door that isn't connected to it can't reach the origin.
            throw new InvalidOperationException(
                $"Origin '{originName}' of Azure Front Door '{frontDoor.Name}' runs in compute environment '{environment.Name}', " +
                $"which is connected to another Azure Front Door through Private Link and has public network access disabled. " +
                $"Call {nameof(AzureFrontDoorPrivateLinkExtensions.WithAzureFrontDoorPrivateLink)} on '{environment.Name}' with '{frontDoor.Name}' to connect it.");
        }

        if (!_targets.TryGetValue(connection, out var target))
        {
            target = CreateTarget(environment);
            _targets.Add(connection, target);
        }

        // The compute environment routes requests by host name, so the origin host header must be the app's own host name,
        // which AddAzureFrontDoor already sets. The certificate name check must stay on for a Private Link origin.
        origin.EnforceCertificateNameCheck = true;
        origin.SharedPrivateLinkResource = new SharedPrivateLinkResourceProperties
        {
            PrivateLinkId = target.EnvironmentId,
            PrivateLinkLocation = target.Location,
            GroupId = connection.Environment.PrivateLinkGroupId,
            RequestMessage = connection.RequestMessage,
        };

        // The compute environment's ingress terminates TLS, so Front Door must probe over HTTPS even when the app's
        // probe endpoint uses HTTP. Routes already forward over HTTPS only.
        originGroup.HealthProbeSettings ??= new HealthProbeSettings();
        originGroup.HealthProbeSettings.ProbeProtocol = HealthProbeProtocol.Https;

        return true;
    }

    /// <summary>
    /// Finishes the template after every origin is added: Private Link needs the Premium SKU, and a connection
    /// that no origin uses is a mistake, because Front Door would never request it.
    /// </summary>
    public void Complete(CdnProfile profile)
    {
        foreach (var connection in frontDoor.Annotations.OfType<AzureFrontDoorPrivateLinkAnnotation>())
        {
            if (!_targets.ContainsKey(connection))
            {
                throw new InvalidOperationException(
                    $"Azure Front Door '{frontDoor.Name}' is connected to compute environment '{connection.Environment.Name}' through Private Link, " +
                    $"but none of its origins run in that environment. Add apps in '{connection.Environment.Name}' with " +
                    $"{nameof(AzureFrontDoorExtensions.WithOrigin)}, or remove the {nameof(AzureFrontDoorPrivateLinkExtensions.WithAzureFrontDoorPrivateLink)} call.");
            }
        }

        if (_targets.Count > 0)
        {
            // Private Link isn't supported for origins of a Standard profile.
            // https://learn.microsoft.com/azure/frontdoor/private-link
            profile.SkuName = CdnSkuName.PremiumAzureFrontDoor;
        }
    }

    private (ProvisioningParameter EnvironmentId, BicepValue<string> Location) CreateTarget(IAzureCdnPrivateLinkTarget environment)
    {
        var environmentIdentifier = Infrastructure.NormalizeBicepIdentifier(environment.Name);
        var environmentId = environment.Id.AsProvisioningParameter(infrastructure, $"{environmentIdentifier}_environment_id");

        // The environment's region is an output of the environment module, so it is only known at deployment. The output
        // can be a display name such as 'West US 2', and Front Door expects 'westus2', so remove the spaces and lowercase
        // the name.
        var environmentLocation = environment.Location.AsProvisioningParameter(infrastructure, $"{environmentIdentifier}_location");
        var privateLinkLocation = new FunctionCallExpression(
            new IdentifierExpression("toLower"),
            new FunctionCallExpression(
                new IdentifierExpression("replace"),
                new IdentifierExpression(environmentLocation.BicepIdentifier),
                new StringLiteralExpression(" "),
                new StringLiteralExpression(string.Empty)));

        return (environmentId, new BicepValue<string>(privateLinkLocation));
    }
}
