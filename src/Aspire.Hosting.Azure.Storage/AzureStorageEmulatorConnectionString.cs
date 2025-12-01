// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

internal static class AzureStorageEmulatorConnectionString
{
    // Default emulator account credentials (these are well-known public values)
    private const string DefaultAccountName = "devstoreaccount1";
    private const string DefaultAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";

    /// <summary>
    /// Creates a connection string for the Azure Storage Emulator using the default account.
    /// </summary>
    public static ReferenceExpression Create(EndpointReference? blobEndpoint = null, EndpointReference? queueEndpoint = null, EndpointReference? tableEndpoint = null)
    {
        return Create(DefaultAccountName, DefaultAccountKey, blobEndpoint, queueEndpoint, tableEndpoint);
    }

    /// <summary>
    /// Creates a connection string for the Azure Storage Emulator using a custom account.
    /// </summary>
    public static ReferenceExpression Create(string accountName, string accountKey, EndpointReference? blobEndpoint = null, EndpointReference? queueEndpoint = null, EndpointReference? tableEndpoint = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(accountName);
        ArgumentException.ThrowIfNullOrEmpty(accountKey);

        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral($"DefaultEndpointsProtocol=http;AccountName={accountName};AccountKey={accountKey};");

        if (blobEndpoint is not null)
        {
            AppendEndpointExpression(builder, "BlobEndpoint", blobEndpoint, accountName);
        }
        if (queueEndpoint is not null)
        {
            AppendEndpointExpression(builder, "QueueEndpoint", queueEndpoint, accountName);
        }
        if (tableEndpoint is not null)
        {
            AppendEndpointExpression(builder, "TableEndpoint", tableEndpoint, accountName);
        }

        return builder.Build();

        static void AppendEndpointExpression(ReferenceExpressionBuilder builder, string key, EndpointReference endpoint, string accountName)
        {
            builder.Append($"{key}={endpoint.Property(EndpointProperty.Scheme)}://{endpoint.Property(EndpointProperty.IPV4Host)}:{endpoint.Property(EndpointProperty.Port)}/{accountName};");
        }
    }
}
