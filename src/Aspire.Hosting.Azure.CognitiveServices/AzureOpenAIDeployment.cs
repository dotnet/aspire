// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure OpenAI Deployment.
/// </summary>
/// <param name="name">The name of the deployment</param>
/// <param name="modelName">The name of the model.</param>
/// <param name="modelVersion">The version of the model.</param>
/// <param name="skuName">The name of the SKU.</param>
/// <param name="skuCapacity">The capacity of the SKU.</param>
public class AzureOpenAIDeployment(string name, string modelName, string modelVersion, string? skuName = null, int? skuCapacity = null)
{
    /// <value>"Standard"</value>>
    private const string DefaultSkuName = "Standard";

    /// <value>8</value>
    private const int DefaultSkuCapacity = 8;

    /// <summary>
    /// Gets the name of the deployment.
    /// </summary>
    public string Name { get; private set; } = name;

    /// <summary>
    /// Gets the name of the model.
    /// </summary>
    public string ModelName { get; private set; } = modelName;

    /// <summary>
    /// Gets the version of the model.
    /// </summary>
    public string ModelVersion { get; private set; } = modelVersion;

    /// <summary>
    /// Gets the name of the SKU.
    /// </summary>
    /// <value>
    /// The default value is <inheritdoc cref="DefaultSkuName"/>.
    /// </value>
    public string SkuName { get; set; } = skuName ?? DefaultSkuName;

    /// <summary>
    /// Gets the capacity of the SKU.
    /// </summary>
    /// <value>
    /// The default value is <inheritdoc cref="DefaultSkuCapacity"/>.
    /// </value>
    public int SkuCapacity { get; set; } = skuCapacity ?? DefaultSkuCapacity;
}
