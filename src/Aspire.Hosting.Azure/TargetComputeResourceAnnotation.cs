// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// An annotation that contains a back-pointer to the original compute resource in the application model.
/// </summary>
/// <param name="computeResource">The original compute resource from the application model.</param>
/// <remarks>
/// This annotation is applied to <see cref="AzureProvisioningResource"/> instances to provide access
/// to the original compute resource and its annotations from within Azure provisioning callbacks
/// such as PublishAsContainerApp and PublishAsAzureAppServiceWebsite.
/// </remarks>
public class TargetComputeResourceAnnotation(
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    IComputeResource computeResource) : IResourceAnnotation
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
{
    /// <summary>
    /// Gets the original compute resource from the application model.
    /// </summary>
#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public IComputeResource ComputeResource { get; } = computeResource ?? throw new ArgumentNullException(nameof(computeResource));
#pragma warning restore ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}