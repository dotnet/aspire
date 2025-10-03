// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// A resource that represents an Azure Data Lake Storage.
/// </summary>
public class AzureDataLakeStorageResource(string name, AzureStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureStorageResource>,
    IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureDataLakeResource.
    /// </summary>
    public AzureStorageResource Parent => storage ?? throw new ArgumentNullException(nameof(storage));

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure DataLake Storage resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression => Parent.GetDataLakeConnectionString();

    internal ReferenceExpression GetConnectionString(string? fileSystemName)
    {
        if (string.IsNullOrEmpty(fileSystemName))
        {
            return ConnectionStringExpression;
        }

        ReferenceExpressionBuilder builder = new();

        if (Parent.IsEmulator)
        {
            throw new InvalidOperationException("Emulator currently does not support data lake.");
        }
        else
        {
            builder.Append($"Endpoint={ConnectionStringExpression}");
        }

        builder.Append($";FileSystemName={fileSystemName}");

        return builder.Build();
    }

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(
        IDictionary<string, object> target,
        string connectionName)
    {
        if (Parent.IsEmulator)
        {
            throw new InvalidOperationException("Emulator currently does not support data lake.");
        }
        else
        {
            target[$"{connectionName}__dataLakeServiceUri"] = Parent.DataLakeEndpoint;
            target[$"{connectionName}__queueServiceUri"] = Parent.QueueEndpoint;
            target[$"{AzureStorageResource.DataLakeConnectionKeyPrefix}__{connectionName}__ServiceUri"] =
                Parent.DataLakeEndpoint;
        }
    }
}
