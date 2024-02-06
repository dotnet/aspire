// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Net.Sockets;
using System.Text;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

public class AzureBicepStorageResource(string name) :
    AzureBicepResource(name, templateResouceName: "Aspire.Hosting.Azure.Bicep.storage.bicep")
{
    /// <summary>
    /// Gets a value indicating whether the Azure Storage resource is running in the local emulator.
    /// </summary>
    public bool IsEmulator => this.IsContainer();

    internal string? GetTableConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(tablePort: GetEmulatorPort("table"))
        : Outputs["tableEndpoint"];

    internal string? GetQueueConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(queuePort: GetEmulatorPort("queue"))
        : Outputs["queueEndpoint"];

    internal string? GetBlobConnectionString() => IsEmulator
        ? AzureStorageEmulatorConnectionString.Create(blobPort: GetEmulatorPort("blob"))
        : Outputs["blobEndpoint"];

    private int GetEmulatorPort(string endpointName) =>
        Annotations
            .OfType<AllocatedEndpointAnnotation>()
            .FirstOrDefault(x => x.Name == endpointName)
            ?.Port
        ?? throw new DistributedApplicationException($"Azure storage resource does not have endpoint annotation with name '{endpointName}'.");
}

public class AzureBicepBlobStorageResource(string name, AzureBicepStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureBicepStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string for the Azure Blob Storage resource.
    /// </summary>
    /// <returns>The connection string for the Azure Blob Storage resource.</returns>
    public string? GetConnectionString() => Parent.GetBlobConnectionString();

    public void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.outputs.blobEndpoint}}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

public class AzureBicepTableStorageResource(string name, AzureBicepStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureBicepStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string for the Azure Blob Storage resource.
    /// </summary>
    /// <returns>The connection string for the Azure Blob Storage resource.</returns>
    public string? GetConnectionString() => Parent.GetTableConnectionString();

    public void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.outputs.tableEndpoint}}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

public class AzureBicepQueueStorageResource(string name, AzureBicepStorageResource storage) : Resource(name),
    IResourceWithConnectionString,
    IResourceWithParent<AzureBicepStorageResource>
{
    /// <summary>
    /// Gets the parent AzureStorageResource of this AzureBlobStorageResource.
    /// </summary>
    public AzureBicepStorageResource Parent => storage;

    /// <summary>
    /// Gets the connection string for the Azure Blob Storage resource.
    ///</summary>
    ///<returns> The connection string for the Azure Blob Storage resource.</returns>
    public string? GetConnectionString() => Parent.GetQueueConnectionString();

    public void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");
        context.Writer.WriteString("connectionString", $"{{{Parent.Name}.outputs.queueEndpoint}}");
        context.Writer.WriteString("parent", Parent.Name);
    }
}

public static class AzureBicepSqlResourceExtensions
{
    public static IResourceBuilder<AzureBicepStorageResource> AddAzureBicepAzureStorage(this IDistributedApplicationBuilder builder, string name)
    {
        var resource = new AzureBicepStorageResource(name);

        return builder.AddResource(resource)
                      // These ambient parameters are only available in development time.
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalId)
                      .WithParameter(AzureBicepResource.KnownParameters.PrincipalType)
                      .WithParameter("storageName", resource.CreateBicepResourceName())
                      .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepStorageResource> UseEmulator(this IResourceBuilder<AzureBicepStorageResource> builder, int? blobPort = null, int? queuePort = null, int? tablePort = null, string? imageTag = null, string? storagePath = null)
    {
        builder.WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "blob", port: blobPort, containerPort: 10000))
               .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "queue", port: queuePort, containerPort: 10001))
               .WithAnnotation(new EndpointAnnotation(ProtocolType.Tcp, name: "table", port: tablePort, containerPort: 10002))
               .WithAnnotation(new ContainerImageAnnotation { Image = "mcr.microsoft.com/azure-storage/azurite", Tag = imageTag ?? "latest" });

        if (storagePath is not null)
        {
            var volumeAnnotation = new VolumeMountAnnotation(storagePath, "/data", VolumeMountType.Bind, false);
            return builder.WithAnnotation(volumeAnnotation);
        }

        return builder;
    }

    public static IResourceBuilder<AzureBicepBlobStorageResource> AddBlob(this IResourceBuilder<AzureBicepStorageResource> builder, string name)
    {
        var resource = new AzureBicepBlobStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepTableStorageResource> AddTable(this IResourceBuilder<AzureBicepStorageResource> builder, string name)
    {
        var resource = new AzureBicepTableStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }

    public static IResourceBuilder<AzureBicepQueueStorageResource> AddQueue(this IResourceBuilder<AzureBicepStorageResource> builder, string name)
    {
        var resource = new AzureBicepQueueStorageResource(name, builder.Resource);

        return builder.ApplicationBuilder.AddResource(resource)
            .WithManifestPublishingCallback(resource.WriteToManifest);
    }
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
