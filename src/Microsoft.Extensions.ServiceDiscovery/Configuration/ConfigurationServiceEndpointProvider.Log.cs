// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.ServiceDiscovery.Configuration;

internal sealed partial class ConfigurationServiceEndpointProvider
{
    private static partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Skipping endpoint resolution for service '{ServiceName}': '{Reason}'.", EventName = "SkippedResolution")]
        public static partial void SkippedResolution(ILogger logger, string serviceName, string reason);

        [LoggerMessage(2, LogLevel.Debug, "Using configuration from path '{Path}' to resolve endpoint '{EndpointName}' for service '{ServiceName}'.", EventName = "UsingConfigurationPath")]
        public static partial void UsingConfigurationPath(ILogger logger, string path, string endpointName, string serviceName);

        [LoggerMessage(3, LogLevel.Debug, "No valid endpoint configuration was found for service '{ServiceName}' from path '{Path}'.", EventName = "ServiceConfigurationNotFound")]
        internal static partial void ServiceConfigurationNotFound(ILogger logger, string serviceName, string path);

        [LoggerMessage(4, LogLevel.Debug, "Endpoints configured for service '{ServiceName}' from path '{Path}': {ConfiguredEndpoints}.", EventName = "ConfiguredEndpoints")]
        internal static partial void ConfiguredEndpoints(ILogger logger, string serviceName, string path, string configuredEndpoints);

        internal static void ConfiguredEndpoints(ILogger logger, string serviceName, string path, IList<ServiceEndpoint> endpoints, int added)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            StringBuilder endpointValues = new();
            for (var i = endpoints.Count - added; i < endpoints.Count; i++)
            {
                if (endpointValues.Length > 0)
                {
                    endpointValues.Append(", ");
                }

                endpointValues.Append(endpoints[i].ToString());
            }

            var configuredEndpoints = endpointValues.ToString();
            ConfiguredEndpoints(logger, serviceName, path, configuredEndpoints);
        }

        [LoggerMessage(5, LogLevel.Debug, "No valid endpoint configuration was found for endpoint '{EndpointName}' on service '{ServiceName}' from path '{Path}'.", EventName = "EndpointConfigurationNotFound")]
        internal static partial void EndpointConfigurationNotFound(ILogger logger, string endpointName, string serviceName, string path);
    }
}
