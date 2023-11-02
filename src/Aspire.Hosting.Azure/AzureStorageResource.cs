// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an Azure Storage resource.
/// </summary>
/// <param name="name">The name of the resource.</param>
public class AzureStorageResource(string name) : Resource(name), IAzureResource
{
    /// <summary>
    /// Gets or sets the URI of the Azure Table Storage resource.
    /// </summary>
    public Uri? TableUri { get; set; }
    
    /// <summary>
    /// Gets or sets the URI of the Azure Storage queue.
    /// </summary>
    public Uri? QueueUri { get; set; }
    
    /// <summary>
    /// Gets or sets the URI of the blob.
    /// </summary>
    public Uri? BlobUri { get; set; }

    /// <summary>
    /// Gets a value indicating whether the Azure Storage resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    internal string? GetTableConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(tablePort: GetEmulatorPort("table"))
        : TableUri?.ToString();

    internal string? GetQueueConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(queuePort: GetEmulatorPort("queue"))
        : QueueUri?.ToString();

    internal string? GetBlobConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(blobPort: GetEmulatorPort("blob"))
        : BlobUri?.ToString();

    private int GetEmulatorPort(string endpointName) =>
        Annotations
            .OfType<AllocatedEndpointAnnotation>()
            .FirstOrDefault(x => x.Name == endpointName)
            ?.Port
        ?? throw new DistributedApplicationException($"Azure storage resource does not have endpoint annotation with name '{endpointName}'.");
}

static file class AzureStorageEmulatorConnectionString
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
