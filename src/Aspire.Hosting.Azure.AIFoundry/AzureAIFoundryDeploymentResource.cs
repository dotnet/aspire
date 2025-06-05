// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AIFoundry;

/// <summary>
/// Represents an Azure AI Services Deployment.
/// </summary>
public class AzureAIFoundryDeploymentResource : Resource, IResourceWithParent<AzureAIFoundryResource>, IResourceWithConnectionString
{
    /// <value>"GlobalStandard"</value>
    private const string DefaultSkuName = "GlobalStandard";

    /// <value>1</value>
    private const int DefaultSkuCapacity = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureAIFoundryDeploymentResource"/> class.
    /// </summary>
    /// <param name="name">The name of the deployment.</param>
    /// <param name="modelName">The name of the model.</param>
    /// <param name="modelVersion">The version of the model.</param>
    /// <param name="format">The format of the model.</param>
    /// <param name="parent">The parent Azure AI Services resource.</param>
    public AzureAIFoundryDeploymentResource(string name, string modelName, string modelVersion, string format, AzureAIFoundryResource parent)
        : base(name)
    {
        DeploymentName = modelName;
        ModelName = modelName;
        ModelVersion = modelVersion;
        Format = format;
        Parent = parent;
    }

    /// <summary>
    /// Gets or sets the name of the deployment.
    /// </summary>
    /// <remarks>
    /// This defaults to <see cref="ModelName"/>, but allows for a different deployment name in Azure.
    /// </remarks>
    public string DeploymentName { get; set; }

    /// <summary>
    /// Gets or sets the name of the model.
    /// </summary>
    public string ModelName { get; set; }

    /// <summary>
    /// Gets or sets the version of the model.
    /// </summary>
    public string ModelVersion { get; set; }

    /// <summary>
    /// Gets or sets the format of deployment model.
    /// </summary>
    public string Format { get; set; }

    /// <summary>
    /// Gets or sets the name of the SKU.
    /// </summary>
    /// <value>
    /// The default value is <inheritdoc cref="DefaultSkuName"/>.
    /// </value>
    public string SkuName { get; set; } = DefaultSkuName;

    /// <summary>
    /// Gets or sets the capacity of the SKU.
    /// </summary>
    /// <value>
    /// The default value is <inheritdoc cref="DefaultSkuCapacity"/>.
    /// </value>
    public int SkuCapacity { get; set; } = DefaultSkuCapacity;

    /// <summary>
    /// Gets the parent Azure AI Services resource.
    /// </summary>
    public AzureAIFoundryResource Parent { get; }

    /// <summary>
    /// Gets the connection string expression for the Azure Event Hub.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        InnerResource == null
            ? Parent.GetConnectionString(DeploymentName)
            : ReferenceExpression.Create($"{Parent.GetConnectionString(DeploymentName)};Model={InnerResource.Model}");

    internal AzureAIFoundryLocalModelResource? InnerResource { get; private set; }

    internal void SetInnerResource(AzureAIFoundryLocalModelResource innerResource)
    {
        // Copy the annotations to the inner resource before making it the inner resource
        foreach (var annotation in Annotations)
        {
            innerResource.Annotations.Add(annotation);
        }

        InnerResource = innerResource;
    }
}
