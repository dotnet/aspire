// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure OpenAI Deployment resource.
/// </summary>
public class AzureOpenAIDeploymentResource : Resource,
    IResourceWithParent<AzureOpenAIResource>
{
    /// <summary>
    /// Represents an Azure OpenAI Deployment resource.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="openai">The <see cref="AzureOpenAIResource"/> that the resource is stored in.</param>
    public AzureOpenAIDeploymentResource(string name, AzureOpenAIResource openai) : base(name)
    {
        Parent = openai;
        Parent.AddDeployment(this);
    }

    /// <summary>
    /// Gets the parent <see cref="AzureOpenAIResource"/> of this <see cref="AzureOpenAIDeploymentResource"/>.
    /// </summary>
    public AzureOpenAIResource Parent { get; }
}
