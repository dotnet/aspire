// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Hosting;
using Aspire.Minio;
using Minio;
using Microsoft.Extensions.Configuration;

namespace Aspire.Extensions.Hosting;

public static class MinioClientBuilderExtensionMethods
{
    public static void AddMinio(this IHostApplicationBuilder builder, string configurationSectionName)
    {

        ArgumentNullException.ThrowIfNull(builder);

        // Obtain the configuration settings for the Minio client.

        MinioConfiguration minioSettings = new();

        builder.Configuration.Bind(configurationSectionName, minioSettings);

        var endpoint = minioSettings.Endpoint;
        var accessKey = minioSettings.AccessKey;
        var secretKey = minioSettings.SecretKey;

        // Add the Minio client to the service collection.
        builder.Services.AddMinio(configureClient => configureClient
            .WithEndpoint(endpoint, 9000)
            .WithSSL(false)
            .WithCredentials(accessKey, secretKey));

        // Add the Minio health check to the service collection.

        // Add the Minio tracing to the service collection.

        // Add the Minio metrics to the service collection.

    }
}
