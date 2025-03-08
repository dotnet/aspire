// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Table Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="storage">The <see cref="AzureStorageResource"/> that the resource is stored in.</param>
public class AzureTableStorageResource(string name, AzureStorageResource storage)
    : Resource(name), IResourceWithConnectionString, IResourceWithParent<AzureStorageResource>, IResourceWithAzureFunctionsConfig
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureTableStorageResource.
    /// </summary>
    public AzureStorageResource Parent => storage ?? throw new ArgumentNullException(nameof(storage));

    /// <summary>
    /// Gets the connection string template for the manifest for the Azure Table Storage resource.
    /// </summary>
    public ReferenceExpression ConnectionStringExpression =>
        Parent.GetTableConnectionString();

    void IResourceWithAzureFunctionsConfig.ApplyAzureFunctionsConfiguration(IDictionary<string, object> target, string connectionName)
    {
        if (Parent.IsEmulator)
        {
            var connectionString = Parent.GetEmulatorConnectionString();
            target[connectionName] = connectionString;
            target[$"{AzureStorageResource.TablesConnectionKeyPrefix}__{connectionName}__ConnectionString"] = connectionString;
        }
        else
        {
            // Injected to support Azure Functions listener.
            target[$"{connectionName}__tableServiceUri"] = Parent.TableEndpoint;
            // Injected to support Aspire client integration for Azure Storage Tables.
            target[$"{AzureStorageResource.TablesConnectionKeyPrefix}__{connectionName}__ServiceUri"] = Parent.TableEndpoint; // Updated for consistency
        }
    }
}
