// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

internal static class AzureStorageEmulatorConnectionString
{
    /// <summary>
    /// Creates a connection string for the Azure Storage Emulator using the default account.
    /// </summary>
    public static ReferenceExpression Create(EndpointReference? blobEndpoint = null, EndpointReference? queueEndpoint = null, EndpointReference? tableEndpoint = null)
    {
        return Create(AzureStorageEmulatorAccount.Default, blobEndpoint, queueEndpoint, tableEndpoint);
    }

    /// <summary>
    /// Creates a connection string for the Azure Storage Emulator using a custom account.
    /// </summary>
    public static ReferenceExpression Create(AzureStorageEmulatorAccount account, EndpointReference? blobEndpoint = null, EndpointReference? queueEndpoint = null, EndpointReference? tableEndpoint = null)
    {
        ArgumentNullException.ThrowIfNull(account);

        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral($"DefaultEndpointsProtocol=http;AccountName={account.Name};AccountKey={account.Key};");

        if (blobEndpoint is not null)
        {
            AppendEndpointExpression(builder, "BlobEndpoint", blobEndpoint, account.Name);
        }
        if (queueEndpoint is not null)
        {
            AppendEndpointExpression(builder, "QueueEndpoint", queueEndpoint, account.Name);
        }
        if (tableEndpoint is not null)
        {
            AppendEndpointExpression(builder, "TableEndpoint", tableEndpoint, account.Name);
        }

        return builder.Build();

        static void AppendEndpointExpression(ReferenceExpressionBuilder builder, string key, EndpointReference endpoint, string accountName)
        {
            builder.Append($"{key}={endpoint.Property(EndpointProperty.Scheme)}://{endpoint.Property(EndpointProperty.IPV4Host)}:{endpoint.Property(EndpointProperty.Port)}/{accountName};");
        }
    }
}
