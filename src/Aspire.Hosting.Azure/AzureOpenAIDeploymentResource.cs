// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure OpenAI Deployment resource.
/// </summary>
public class AzureOpenDeploymentResource : Resource,
    IResourceWithParent<AzureOpenAIResource>
{
    /// <summary>
    /// Represents an Azure OpenAI Deployment resource.
    /// </summary>
    /// <param name="name">The name of the resource.</param>
    /// <param name="openai">The <see cref="AzureOpenAIResource"/> that the resource is stored in.</param>
    /// <param name="arguments">The arguments of the deployment.</param>
    public AzureOpenDeploymentResource(string name, AzureOpenAIResource openai, IReadOnlyCollection<KeyValuePair<string, object?>> arguments) : base(name)
    {
        Parent = openai;
        Arguments = arguments;
        Parent.AddDeployment(this);
    }

    /// <summary>
    /// Gets the parent <see cref="AzureOpenAIResource"/> of this <see cref="AzureOpenDeploymentResource"/>.
    /// </summary>
    public AzureOpenAIResource Parent { get; }

    /// <summary>
    /// Gets the list of arguments of the deployment resource.
    /// </summary>
    public IReadOnlyCollection<KeyValuePair<string, object?>> Arguments { get; private set; }
}
