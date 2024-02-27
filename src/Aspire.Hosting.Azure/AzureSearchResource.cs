// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Search resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureSearchResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.search.bicep"),
    IResourceWithConnectionString
{
    /// <summary>
    /// Gets the "connectionString" reference from the secret outputs of the Azure Search resource.
    /// </summary>
    public BicepSecretOutputReference ConnectionString => new("connectionString", this);

    /// <summary>
    /// Gets the connection string template for the manifest for the resource.
    /// </summary>
    public string ConnectionStringExpression => ConnectionString.ValueExpression;

    /// <summary>
    /// Gets the connection string for the resource.
    /// </summary>
    /// <returns>The connection string for the resource.</returns>
    public string? GetConnectionString() => ConnectionString.Value;
}

