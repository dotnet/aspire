// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

using System.Diagnostics.Tracing;
using Aspire.Hosting.Dcp;

[EventSource(Name = "Microsoft-Aspire-Hosting")]
internal sealed class AspireEventSource : EventSource
{
    public static readonly AspireEventSource Instance = new AspireEventSource();

    [Event(1, Level = EventLevel.Informational, Message = "DCP host is starting...")]
    public void DcpHostStartupStart()
    {
        Console.WriteLine ($"Event 1: DCP host is starting...");
        if (IsEnabled())
        {
            WriteEvent(1);
        }
    }

    [Event(2, Level = EventLevel.Informational, Message = "DCP host has started")]
    public void DcpHostStartupStop()
    {
        Console.WriteLine ($"Event 2: DCP host has started");
        if (IsEnabled())
        {
            WriteEvent(2);
        }
    }

    [Event(3, Level = EventLevel.Informational, Message = "Container runtime health check is starting...")]
    public void ContainerRuntimeHealthCheckStart()
    {
        Console.WriteLine("Event 3: Container runtime health check is starting...");
        if (IsEnabled())
        {
            WriteEvent(3);
        }
    }

    [Event(4, Level = EventLevel.Informational, Message = "Container runtime health check completed")]
    public void ContainerRuntimeHealthCheckStop()
    {
        Console.WriteLine ($"Event 4: Container runtime health check completed");
        if (IsEnabled())
        {
            WriteEvent(4);
        }
    }

    [Event(5, Level = EventLevel.Informational, Message = "DistributedApplication build is starting...")]
    public void DistributedApplicationBuildStart()
    {
        Console.WriteLine ($"Event 5: DistributedApplication build is starting...");
        if (IsEnabled())
        {
            WriteEvent(5);
        }
    }

    [Event(6, Level = EventLevel.Informational, Message = "DistributedApplication build completed")]
    public void DistributedApplicationBuildStop()
    {
        Console.WriteLine ($"Event 6: DistributedApplication build completed");
        if (IsEnabled())
        {
            WriteEvent(6);
        }
    }

    [Event(7, Level = EventLevel.Informational, Message = "DCP API server is starting...")]
    public void DcpApiServerLaunchStart()
    {
        Console.WriteLine ($"Event 7: DCP API server is starting...");
        if (IsEnabled())
        {
            WriteEvent(7);
        }
    }

    [Event(8, Level = EventLevel.Informational, Message = "DCP API server has started")]
    public void DcpApiServerLaunchStop()
    {
        System.Console.WriteLine("Event 8: DCP API server has started");
        if (IsEnabled())
        {
            WriteEvent(8);
        }
    }

    [Event(9, Level = EventLevel.Informational, Message = "DCP logging socket is being created...")]
    public void DcpLogSocketCreateStart()
    {
        Console.WriteLine ($"Event 9: DCP logging socket is being created...");
        if (IsEnabled())
        {
            WriteEvent(9);
        }
    }

    [Event(10, Level = EventLevel.Informational, Message = "DCP logging socket has been created")]
    public void DcpLogSocketCreateStop()
    {
        Console.WriteLine ($"Event 10: DCP logging socket has been created");
        if (IsEnabled())
        {
            WriteEvent(10);
        }
    }

    [Event(11, Level = EventLevel.Informational, Message = "A process is starting...")]
    public void ProcessLaunchStart(string executablePath, string arguments)
    {
        Console.WriteLine ($"Event 11: A process is starting...");
        if (IsEnabled())
        {
            WriteEvent(11, executablePath, arguments);
        }
    }

    [Event(12, Level = EventLevel.Informational, Message = "Process has been started")]
    public void ProcessLaunchStop(string executablePath, string arguments)
    {
        Console.WriteLine ($"Event 12: Process has been started");
        if (IsEnabled())
        {
            WriteEvent(12, executablePath, arguments);
        }
    }

    [Event(13, Level = EventLevel.Informational, Message = "DCP API call starting...")]
    public void DcpApiCallStart(DcpApiOperationType operationType, string resourceType)
    {
        Console.WriteLine ($"Event 13: DCP API call starting...");
        if (IsEnabled())
        {
            WriteEvent(13, operationType, resourceType);
        }
    }

    [Event(14, Level = EventLevel.Informational, Message = "DCP API call completed")]
    public void DcpApiCallStop(DcpApiOperationType operationType, string resourceType)
    {
        Console.WriteLine ($"Event 14: DCP API call completed");
        if (IsEnabled())
        {
            WriteEvent(14, operationType, resourceType);
        }
    }

    [Event(15, Level = EventLevel.Informational, Message = "DCP API call is being retried...")]
    public void DcpApiCallRetry(DcpApiOperationType operationType, string resourceType)
    {
        Console.WriteLine ($"Event 15: DCP API call is being retried...");
        if (IsEnabled())
        {
            WriteEvent(15, operationType, resourceType);
        }
    }

    [Event(16, Level = EventLevel.Error, Message = "DCP API call timed out")]
    public void DcpApiCallTimeout(DcpApiOperationType operationType, string resourceType)
    {
        Console.WriteLine ($"Event 16: DCP API call timed out");
        if (IsEnabled())
        {
            WriteEvent(16, operationType, resourceType);
        }
    }

    [Event(17, Level = EventLevel.Informational, Message = "DCP application model creation starting...")]
    public void DcpModelCreationStart()
    {
        Console.WriteLine ($"Event 17: DCP application model creation starting...");
        if (IsEnabled())
        {
            WriteEvent(17);
        }
    }

    [Event(18, Level = EventLevel.Informational, Message = "DCP application model creation completed")]
    public void DcpModelCreationStop()
    {
        Console.WriteLine ($"Event 18: DCP application model creation completed");
        if (IsEnabled())
        {
            WriteEvent(18);
        }
    }

    [Event(19, Level = EventLevel.Informational, Message = "DCP Service object creation starting...")]
    public void DcpServicesCreationStart()
    {
        Console.WriteLine ($"Event 19: DCP Service object creation starting...");
        if (IsEnabled())
        {
            WriteEvent(19);
        }
    }

    [Event(20, Level = EventLevel.Informational, Message = "DCP Service object creation completed")]
    public void DcpServicesCreationStop()
    {
        Console.WriteLine ($"Event 20: DCP Service object creation completed");
        if (IsEnabled())
        {
            WriteEvent(20);
        }
    }

    [Event(21, Level = EventLevel.Informational, Message = "DCP Container object creation starting...")]
    public void DcpContainersCreateStart()
    {
        Console.WriteLine ($"Event 21: DCP Container object creation starting...");
        if (IsEnabled())
        {
            WriteEvent(21);
        }
    }

    [Event(22, Level = EventLevel.Informational, Message = "DCP Container object creation completed")]
    public void DcpContainersCreateStop()
    {
        Console.WriteLine ($"Event 22: DCP Container object creation completed");
        if (IsEnabled())
        {
            WriteEvent(22);
        }
    }

    [Event(23, Level = EventLevel.Informational, Message = "DCP Executable object creation starting...")]
    public void DcpExecutablesCreateStart()
    {
        Console.WriteLine ($"Event 23: DCP Executable object creation starting...");
        if (IsEnabled())
        {
            WriteEvent(23);
        }
    }

    [Event(24, Level = EventLevel.Informational, Message = "DCP Executable object creation completed")]
    public void DcpExecutablesCreateStop()
    {
        Console.WriteLine ($"Event 24: DCP Executable object creation completed");
        if (IsEnabled())
        {
            WriteEvent(24);
        }
    }

    [Event(25, Level = EventLevel.Informational, Message = "DCP application model cleanup starting...")]
    public void DcpModelCleanupStart()
    {
        Console.WriteLine ($"Event 25: DCP application model cleanup starting...");
        if (IsEnabled())
        {
            WriteEvent(25);
        }
    }

    [Event(26, Level = EventLevel.Informational, Message = "DCP application model cleanup completed")]
    public void DcpModelCleanupStop()
    {
        Console.WriteLine ($"Event 26: DCP application model cleanup completed");
        if (IsEnabled())
        {
            WriteEvent(26);
        }
    }

    [Event(27, Level = EventLevel.Informational, Message = "Application before-start hooks running...")]
    public void AppBeforeStartHooksStart()
    {
        Console.WriteLine ($"Event 27: Application before-start hooks running...");
        if (IsEnabled())
        {
            WriteEvent(27);
        }
    }

    [Event(28, Level = EventLevel.Informational, Message = "Application before-start hooks completed")]
    public void AppBeforeStartHooksStop()
    {
        Console.WriteLine ($"Event 28: Application before-start hooks completed");
        if (IsEnabled())
        {
            WriteEvent(28);
        }
    }

    [Event(29, Level = EventLevel.Informational, Message = "DCP version check is starting...")]
    public void DcpVersionCheckStart()
    {
        Console.WriteLine ($"Event 29: DCP version check is starting...");
        if (IsEnabled())
        {
            WriteEvent(29);
        }
    }

    [Event(30, Level = EventLevel.Informational, Message = "DCP version check completed")]
    public void DcpVersionCheckStop()
    {
        Console.WriteLine ($"Event 30: DCP version check completed");
        if (IsEnabled())
        {
            WriteEvent(30);
        }
    }
}
