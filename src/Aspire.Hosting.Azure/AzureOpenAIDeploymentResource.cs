// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;

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
    /// <param name="modelName"></param>
    /// <param name="modelVersion"></param>
    /// <param name="skuName"></param>
    /// <param name="skuCapacity"></param>
    /// <param name="openai">The <see cref="AzureOpenAIResource"/> that the resource is stored in.</param>
    public AzureOpenAIDeploymentResource(string name, AzureOpenAIResource openai, string modelName, string modelVersion, string skuName = "Standard", int skuCapacity = 2) : base(name)
    {
        Parent = openai;
        ModelName = modelName;
        ModelVersion = modelVersion;
        SkuName = skuName;
        SkuCapacity = skuCapacity;
        Parent.AddDeployment(this);
    }

    /// <summary>
    /// Gets the parent <see cref="AzureOpenAIResource"/> of this <see cref="AzureOpenAIDeploymentResource"/>.
    /// </summary>
    public AzureOpenAIResource Parent { get; }

    /// <summary>
    /// Gets the model name.
    /// </summary>
    public string ModelName { get; }

    /// <summary>
    /// Gets the model version.
    /// </summary>
    public string ModelVersion { get; }

    /// <summary>
    /// Gets the SKU name.
    /// </summary>
    public string SkuName { get; }

    /// <summary>
    /// Gets the SKU capacity.
    /// </summary>
    public int SkuCapacity { get; }

    internal JsonNode ToJsonNode()
    {
        return new JsonObject
        {
            ["name"] = Name,
            ["sku"] = new JsonObject
            {
                ["name"] = SkuName,
                ["capacity"] = SkuCapacity
            },
            ["model"] = new JsonObject
            {
                ["format"] = "OpenAI",
                ["name"] = ModelName,
                ["version"] = ModelVersion
            }
        };
    }
}
