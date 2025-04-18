// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIRECOMPUTE001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Kubernetes;

/// <summary>
/// Represents a Kubernetes environment resource that can host application resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="KubernetesEnvironmentResource"/> class.
/// </remarks>
/// <param name="name">The name of the Kubernetes environment.</param>
public sealed class KubernetesEnvironmentResource(string name) : Resource(name), IComputeEnvironmentResource
{
}
