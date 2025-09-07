// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

internal static class AzureStorageEmulatorConnectionString
{
    // Use defaults from https://learn.microsoft.com/azure/storage/common/storage-configure-connection-string#connect-to-the-emulator-account-using-the-shortcut
    private const string ConnectionStringHeader = "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;";

    public static ReferenceExpression Create(EndpointReference? blobEndpoint = null, EndpointReference? queueEndpoint = null, EndpointReference? tableEndpoint = null)
    {
        var builder = new ReferenceExpressionBuilder();
        builder.AppendLiteral(ConnectionStringHeader);

        if (blobEndpoint is not null)
        {
            AppendEndpointExpression(builder, "BlobEndpoint", blobEndpoint);
        }
        if (queueEndpoint is not null)
        {
            AppendEndpointExpression(builder, "QueueEndpoint", queueEndpoint);
        }
        if (tableEndpoint is not null)
        {
            AppendEndpointExpression(builder, "TableEndpoint", tableEndpoint);
        }

        return builder.Build();

        static void AppendEndpointExpression(ReferenceExpressionBuilder builder, string key, EndpointReference endpoint)
        {
            builder.Append($"{key}={endpoint.Property(EndpointProperty.Scheme)}://{endpoint.Property(EndpointProperty.IPV4Host)}:{endpoint.Property(EndpointProperty.Port)}/devstoreaccount1;");
        }
    }
}
