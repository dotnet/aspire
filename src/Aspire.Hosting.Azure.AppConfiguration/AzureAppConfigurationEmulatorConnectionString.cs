// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure.AppConfiguration;

internal static class AzureAppConfigurationEmulatorConnectionString
{
    public static ReferenceExpression Create(EndpointReference endpoint, AzureAppConfigurationEmulatorOptions options)
    {
        if (options.AnonymousAccessEnabled)
        {
            return ReferenceExpression.Create($"Endpoint={endpoint.Property(EndpointProperty.Url)};Id=anonymous;Secret=abcdefghijklmnopqrstuvwxyz1234567890;Anonymous=True");
        }
        throw new InvalidOperationException("Cannot create connection string for Azure App Configuration emulator. No authentication method is configured.");
    }
}
