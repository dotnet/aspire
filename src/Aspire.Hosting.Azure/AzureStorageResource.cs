// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Storage resource.
/// </summary>
/// <param name="name"></param>
/// <param name="configureConstruct"></param>
public class AzureStorageConstructResource(string name, Action<ResourceModuleConstruct> configureConstruct) :
    AzureConstructResource(name, configureConstruct),
    IResourceWithEndpoints
{
    private EndpointReference EmulatorBlobEndpoint => new(this, "blob");
    private EndpointReference EmulatorQueueEndpoint => new(this, "queue");
    private EndpointReference EmulatorTableEndpoint => new(this, "table");

    /// <summary>
    /// Gets the "blobEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference BlobEndpoint => new("blobEndpoint", this);

    /// <summary>
    /// Gets the "queueEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference QueueEndpoint => new("queueEndpoint", this);

    /// <summary>
    /// Gets the "tableEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference TableEndpoint => new("tableEndpoint", this);

    /// <summary>
    /// Gets a value indicating whether the Azure Storage resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    internal string? GetTableConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(tablePort: EmulatorTableEndpoint.Port)
        : TableEndpoint.Value;

    internal async ValueTask<string?> GetTableConnectionStringAsync(CancellationToken cancellationToken = default) => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(tablePort: EmulatorTableEndpoint.Port)
        : await TableEndpoint.GetValueAsync(cancellationToken).ConfigureAwait(false);

    internal string? GetQueueConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(queuePort: EmulatorQueueEndpoint.Port)
        : QueueEndpoint.Value;

    internal async ValueTask<string?> GetQueueConnectionStringAsync(CancellationToken cancellationToken = default) => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(queuePort: EmulatorQueueEndpoint.Port)
        : await QueueEndpoint.GetValueAsync(cancellationToken).ConfigureAwait(false);

    internal string? GetBlobConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(blobPort: EmulatorBlobEndpoint.Port)
        : BlobEndpoint.Value;

    internal async ValueTask<string?> GetBlobConnectionStringAsync(CancellationToken cancellationToken = default) => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(blobPort: EmulatorBlobEndpoint.Port)
        : await BlobEndpoint.GetValueAsync(cancellationToken).ConfigureAwait(false);
}

/// <summary>
/// Represents an Azure Storage resource.
/// </summary>
/// <param name="name"></param>
public class AzureStorageResource(string name) :
    AzureBicepResource(name, templateResourceName: "Aspire.Hosting.Azure.Bicep.storage.bicep"),
    IResourceWithEndpoints
{
    // Emulator container endpoints
    private EndpointReference EmulatorBlobEndpoint => new(this, "blob");
    private EndpointReference EmulatorQueueEndpoint => new(this, "queue");
    private EndpointReference EmulatorTableEndpoint => new(this, "table");

    /// <summary>
    /// Gets the "blobEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference BlobEndpoint => new("blobEndpoint", this);

    /// <summary>
    /// Gets the "queueEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference QueueEndpoint => new("queueEndpoint", this);

    /// <summary>
    /// Gets the "tableEndpoint" output reference from the bicep template for the Azure Storage resource.
    /// </summary>
    public BicepOutputReference TableEndpoint => new("tableEndpoint", this);

    /// <summary>
    /// Gets a value indicating whether the Azure Storage resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    internal string? GetTableConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(tablePort: EmulatorTableEndpoint.Port)
        : TableEndpoint.Value;

    internal string? GetQueueConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(queuePort: EmulatorQueueEndpoint.Port)
        : QueueEndpoint.Value;

    internal string? GetBlobConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(blobPort: EmulatorBlobEndpoint.Port)
        : BlobEndpoint.Value;
}

file static class AzureStorageEmulatorConnectionString
{
    // Use defaults from https://learn.microsoft.com/azure/storage/common/storage-configure-connection-string#connect-to-the-emulator-account-using-the-shortcut
    private const string ConnectionStringHeader = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;";
    private const string BlobEndpointTemplate = "BlobEndpoint=http://127.0.0.1:{0}/devstoreaccount1;";
    private const string QueueEndpointTemplate = "QueueEndpoint=http://127.0.0.1:{0}/devstoreaccount1;";
    private const string TableEndpointTemplate = "TableEndpoint=http://127.0.0.1:{0}/devstoreaccount1;";

    public static string Create(int? blobPort = null, int? queuePort = null, int? tablePort = null)
    {
        var builder = new StringBuilder(ConnectionStringHeader);

        if (blobPort is not null)
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, BlobEndpointTemplate, blobPort);
        }
        if (queuePort is not null)
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, QueueEndpointTemplate, queuePort);
        }
        if (tablePort is not null)
        {
            builder.AppendFormat(CultureInfo.InvariantCulture, TableEndpointTemplate, tablePort);
        }

        return builder.ToString();
    }
}
