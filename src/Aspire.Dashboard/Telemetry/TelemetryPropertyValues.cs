// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Telemetry;

public static class TelemetryPropertyValues
{
    private const string CustomResourceCommand = "custom-command";
    private const string CustomResourceType = "custom-resource-type";
    
    // Known resource command constants
    private const string StartCommand = "resource-start";
    private const string StopCommand = "resource-stop"; 
    private const string RestartCommand = "resource-restart";

    public static string GetCommandNameTelemetryValue(string commandName)
    {
        return IsKnownCommand(commandName)
            ? commandName
            : CustomResourceCommand;
    }
    
    private static bool IsKnownCommand(string command)
    {
        return command == StartCommand || command == StopCommand || command == RestartCommand;
    }

    public static string GetResourceTypeTelemetryValue(string resourceType, bool supportsDetailedTelemetry)
    {
        return supportsDetailedTelemetry
            ? resourceType
            : CustomResourceType;
    }
}
