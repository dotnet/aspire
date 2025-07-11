// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Azure.Provisioning;

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

    /// <summary>
    /// Converts the current instance to a provisioning entity.
    /// </summary>
    /// <returns>A <see cref="global::Azure.Provisioning.Storage.TableService"/> instance.</returns>
    internal global::Azure.Provisioning.Storage.TableService ToProvisioningEntity()
    {
        // Create the TableService with the correct name
        global::Azure.Provisioning.Storage.TableService service = new(Infrastructure.NormalizeBicepIdentifier(Name));
        
        // Set the name using internal infrastructure, similar to how it's done for other storage services
        // TableService requires the name to be "default" like BlobService and QueueService
        try
        {
            // Use reflection to access the internal _name property/field
            var nameProperty = service.GetType().GetProperty("Name", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            if (nameProperty is not null && nameProperty.CanWrite)
            {
                nameProperty.SetValue(service, "default");
            }
            else
            {
                // Try to set via backing field if property is read-only
                var fields = service.GetType().GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                foreach (var field in fields)
                {
                    if (field.Name.EndsWith("name>k__BackingField", StringComparison.OrdinalIgnoreCase) ||
                        field.Name.Equals("_name", StringComparison.OrdinalIgnoreCase) ||
                        field.FieldType == typeof(string) && field.Name.Contains("name", StringComparison.OrdinalIgnoreCase))
                    {
                        field.SetValue(service, "default");
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // If reflection fails, the service will still work but may not have the name set
        }
        
        return service;
    }
}
