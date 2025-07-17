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
        if (IsEnabled())
        {
            WriteEvent(1);
        }
    }

    [Event(2, Level = EventLevel.Informational, Message = "DCP host has started")]
    public void DcpHostStartupStop()
    {
        if (IsEnabled())
        {
            WriteEvent(2);
        }
    }

    [Event(3, Level = EventLevel.Informational, Message = "Container runtime health check is starting...")]
    public void ContainerRuntimeHealthCheckStart()
    {
        if (IsEnabled())
        {
            WriteEvent(3);
        }
    }

    [Event(4, Level = EventLevel.Informational, Message = "Container runtime health check completed")]
    public void ContainerRuntimeHealthCheckStop()
    {
        if (IsEnabled())
        {
            WriteEvent(4);
        }
    }

    [Event(5, Level = EventLevel.Informational, Message = "DistributedApplication build is starting...")]
    public void DistributedApplicationBuildStart()
    {
        if (IsEnabled())
        {
            WriteEvent(5);
        }
    }

    [Event(6, Level = EventLevel.Informational, Message = "DistributedApplication build completed")]
    public void DistributedApplicationBuildStop()
    {
        if (IsEnabled())
        {
            WriteEvent(6);
        }
    }

    [Event(7, Level = EventLevel.Informational, Message = "DCP API server is starting...")]
    public void DcpApiServerLaunchStart()
    {
        if (IsEnabled())
        {
            WriteEvent(7);
        }
    }

    [Event(8, Level = EventLevel.Informational, Message = "DCP API server has started")]
    public void DcpApiServerLaunchStop()
    {
        if (IsEnabled())
        {
            WriteEvent(8);
        }
    }

    [Event(9, Level = EventLevel.Informational, Message = "DCP logging socket is being created...")]
    public void DcpLogSocketCreateStart()
    {
        if (IsEnabled())
        {
            WriteEvent(9);
        }
    }

    [Event(10, Level = EventLevel.Informational, Message = "DCP logging socket has been created")]
    public void DcpLogSocketCreateStop()
    {
        if (IsEnabled())
        {
            WriteEvent(10);
        }
    }

    [Event(11, Level = EventLevel.Informational, Message = "A process is starting...")]
    public void ProcessLaunchStart(string executablePath, string arguments)
    {
        if (IsEnabled())
        {
            WriteEvent(11, executablePath, arguments);
        }
    }

    [Event(12, Level = EventLevel.Informational, Message = "Process has been started")]
    public void ProcessLaunchStop(string executablePath, string arguments)
    {
        if (IsEnabled())
        {
            WriteEvent(12, executablePath, arguments);
        }
    }

    [Event(13, Level = EventLevel.Informational, Message = "DCP API call starting...")]
    public void DcpApiCallStart(DcpApiOperationType operationType, string resourceType)
    {
        if (IsEnabled())
        {
            WriteEvent(13, operationType, resourceType);
        }
    }

    [Event(14, Level = EventLevel.Informational, Message = "DCP API call completed")]
    public void DcpApiCallStop(DcpApiOperationType operationType, string resourceType)
    {
        if (IsEnabled())
        {
            WriteEvent(14, operationType, resourceType);
        }
    }

    [Event(15, Level = EventLevel.Informational, Message = "DCP API call is being retried...")]
    public void DcpApiCallRetry(DcpApiOperationType operationType, string resourceType)
    {
        if (IsEnabled())
        {
            WriteEvent(15, operationType, resourceType);
        }
    }

    [Event(16, Level = EventLevel.Error, Message = "DCP API call timed out")]
    public void DcpApiCallTimeout(DcpApiOperationType operationType, string resourceType)
    {
        if (IsEnabled())
        {
            WriteEvent(16, operationType, resourceType);
        }
    }

    [Event(17, Level = EventLevel.Informational, Message = "DCP application model creation starting...")]
    public void DcpModelCreationStart()
    {
        if (IsEnabled())
        {
            WriteEvent(17);
        }
    }

    [Event(18, Level = EventLevel.Informational, Message = "DCP application model creation completed")]
    public void DcpModelCreationStop()
    {
        if (IsEnabled())
        {
            WriteEvent(18);
        }
    }

    [Event(19, Level = EventLevel.Informational, Message = "DCP Service object creation starting...")]
    public void DcpServicesCreationStart()
    {
        if (IsEnabled())
        {
            WriteEvent(19);
        }
    }

    [Event(20, Level = EventLevel.Informational, Message = "DCP Service object creation completed")]
    public void DcpServicesCreationStop()
    {
        if (IsEnabled())
        {
            WriteEvent(20);
        }
    }

    [Event(21, Level = EventLevel.Informational, Message = "DCP Container object creation starting...")]
    public void DcpContainersCreateStart()
    {
        if (IsEnabled())
        {
            WriteEvent(21);
        }
    }

    [Event(22, Level = EventLevel.Informational, Message = "DCP Container object creation completed")]
    public void DcpContainersCreateStop()
    {
        if (IsEnabled())
        {
            WriteEvent(22);
        }
    }

    [Event(23, Level = EventLevel.Informational, Message = "DCP Executable object creation starting...")]
    public void DcpExecutablesCreateStart()
    {
        if (IsEnabled())
        {
            WriteEvent(23);
        }
    }

    [Event(24, Level = EventLevel.Informational, Message = "DCP Executable object creation completed")]
    public void DcpExecutablesCreateStop()
    {
        if (IsEnabled())
        {
            WriteEvent(24);
        }
    }

    [Event(25, Level = EventLevel.Informational, Message = "DCP application model cleanup starting...")]
    public void DcpModelCleanupStart()
    {
        if (IsEnabled())
        {
            WriteEvent(25);
        }
    }

    [Event(26, Level = EventLevel.Informational, Message = "DCP application model cleanup completed")]
    public void DcpModelCleanupStop()
    {
        if (IsEnabled())
        {
            WriteEvent(26);
        }
    }

    [Event(27, Level = EventLevel.Informational, Message = "Application before-start hooks running...")]
    public void AppBeforeStartHooksStart()
    {
        if (IsEnabled())
        {
            WriteEvent(27);
        }
    }

    [Event(28, Level = EventLevel.Informational, Message = "Application before-start hooks completed")]
    public void AppBeforeStartHooksStop()
    {
        if (IsEnabled())
        {
            WriteEvent(28);
        }
    }

    [Event(29, Level = EventLevel.Informational, Message = "DCP version check is starting...")]
    public void DcpVersionCheckStart()
    {
        if (IsEnabled())
        {
            WriteEvent(29);
        }
    }

    [Event(30, Level = EventLevel.Informational, Message = "DCP version check completed")]
    public void DcpVersionCheckStop()
    {
        if (IsEnabled())
        {
            WriteEvent(30);
        }
    }

    [Event(31, Level = EventLevel.Informational, Message = "DCP Container Executable object creation starting...")]
    public void DcpContainerExecutablesCreateStart()
    {
        if (IsEnabled())
        {
            WriteEvent(31);
        }
    }

    [Event(32, Level = EventLevel.Informational, Message = "DCP Container Executable object creation completed")]
    public void DcpContainerExecutablesCreateStop()
    {
        if (IsEnabled())
        {
            WriteEvent(32);
        }
    }

    [Event(33, Level = EventLevel.Informational, Message = "DCP snapshotable resources creation starting...")]
    public void DcpSnapshotableResourcesCreateStart()
    {
        if (IsEnabled())
        {
            WriteEvent(33);
        }
    }

    [Event(34, Level = EventLevel.Informational, Message = "DCP snapshotable resources creation completed")]
    public void DcpSnapshotableResourcesCreateStop()
    {
        if (IsEnabled())
        {
            WriteEvent(34);
        }
    }
}
