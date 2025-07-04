// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREAZURE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using System.Diagnostics.CodeAnalysis;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents the root Azure deployment target for an Aspire application.
/// Emits a <c>main.bicep</c> that aggregates all provisionable resources.
/// </summary>
[Experimental("ASPIREAZURE001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class AzureEnvironmentResource : Resource
{
    /// <summary>
    /// Gets or sets the Azure location that the resources will be deployed to.
    /// </summary>
    public ParameterResource Location { get; set; }

    /// <summary>
    /// Gets or sets the Azure resource group name that the resources will be deployed to.
    /// </summary>
    public ParameterResource ResourceGroupName { get; set; }

    /// <summary>
    /// Gets or sets the Azure principal ID that will be used to deploy the resources.
    /// </summary>
    public ParameterResource PrincipalId { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureEnvironmentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the Azure environment resource.</param>
    /// <param name="location">The Azure location that the resources will be deployed to.</param>
    /// <param name="resourceGroupName">The Azure resource group name that the resources will be deployed to.</param>
    /// <param name="principalId">The Azure principal ID that will be used to deploy the resources.</param>
    /// <exception cref="ArgumentNullException">Thrown when the name is null or empty.</exception>
    /// <exception cref="ArgumentException">Thrown when the name is invalid.</exception>
    public AzureEnvironmentResource(string name, ParameterResource location, ParameterResource resourceGroupName, ParameterResource principalId) : base(name)
    {
        Annotations.Add(new PublishingCallbackAnnotation(PublishAsync));

        Location = location;
        ResourceGroupName = resourceGroupName;
        PrincipalId = principalId;
    }

    private Task PublishAsync(PublishingContext context)
    {
        var azureProvisioningOptions = context.Services.GetRequiredService<IOptions<AzureProvisioningOptions>>();

        var azureCtx = new AzurePublishingContext(
            context.OutputPath,
            azureProvisioningOptions.Value,
            context.Logger,
            context.ActivityReporter);

        return azureCtx.WriteModelAsync(context.Model, this);
    }
}
