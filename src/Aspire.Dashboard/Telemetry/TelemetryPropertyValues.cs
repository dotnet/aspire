// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;

namespace Aspire.Dashboard.Telemetry;

public static class TelemetryPropertyValues
{
    private const string CustomResourceCommand = "custom-command";
    private const string CustomResourceType = "custom-resource-type";

    public static string GetCommandNameTelemetryValue(string commandName)
    {
        return KnownResourceCommands.IsKnownCommand(commandName)
            ? commandName
            : CustomResourceCommand;
    }

    public static string GetResourceTypeTelemetryValue(string resourceType, bool supportsDetailedTelemetry)
    {
        return supportsDetailedTelemetry
            ? resourceType
            : CustomResourceType;
    }
}
