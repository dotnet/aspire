// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ServiceDiscovery.Internal;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

internal sealed partial class ConfigurationServiceEndPointResolver
{
    private sealed partial class Log
    {
        [LoggerMessage(1, LogLevel.Debug, "Skipping endpoint resolution for service '{ServiceName}': '{Reason}'.", EventName = nameof(SkippedResolution))]  
        public static partial void SkippedResolution(ILogger logger, string serviceName, string reason);

        [LoggerMessage(2, LogLevel.Debug, "Matching endpoints using endpoint names for service '{ServiceName}' since endpoint names are specified in configuration.", EventName = nameof(MatchingEndPointNames))]
        public static partial void MatchingEndPointNames(ILogger logger, string serviceName);

        [LoggerMessage(3, LogLevel.Debug, "Ignoring endpoints using endpoint names for service '{ServiceName}' since no endpoint names are specified in configuration.", EventName = nameof(IgnoringEndPointNames))]
        public static partial void IgnoringEndPointNames(ILogger logger, string serviceName);

        public static void EndPointNameMatchSelection(ILogger logger, string serviceName, bool matchEndPointNames)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            if (matchEndPointNames)
            {
                MatchingEndPointNames(logger, serviceName);
            }
            else
            {
                IgnoringEndPointNames(logger, serviceName);
            }
        }

        [LoggerMessage(4, LogLevel.Debug, "Using configuration from path '{Path}' to resolve endpoints for service '{ServiceName}'.", EventName = nameof(UsingConfigurationPath))]
        public static partial void UsingConfigurationPath(ILogger logger, string path, string serviceName);

        [LoggerMessage(5, LogLevel.Debug, "No endpoints configured for service '{ServiceName}' at path '{Path}'.", EventName = nameof(ConfigurationNotFound))]
        internal static partial void ConfigurationNotFound(ILogger logger, string serviceName, string path);

        [LoggerMessage(6, LogLevel.Debug, "Endpoints configured for service '{ServiceName}' at path '{Path}': {ConfiguredEndPoints}.", EventName = nameof(ConfiguredEndPoints))]
        internal static partial void ConfiguredEndPoints(ILogger logger, string serviceName, string path, string configuredEndPoints);
        public static void ConfiguredEndPoints(ILogger logger, string serviceName, string path, List<string> values, List<ServiceNameParts> parsedValues)
        {
            if (!logger.IsEnabled(LogLevel.Debug))
            {
                return;
            }

            StringBuilder endpointValues = new();
            for (var i = 0; i < values.Count; i++)
            {
                if (endpointValues.Length > 0)
                {
                    endpointValues.Append(", ");
                }

                endpointValues.Append(CultureInfo.InvariantCulture, $"'{values[i]}': [{parsedValues[i]}]");
            }

            var configuredEndPoints = endpointValues.ToString();
            ConfiguredEndPoints(logger, serviceName, path, configuredEndPoints);
        }
    }
}
