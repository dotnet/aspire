// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

// Used for the manifest publisher to dynamically allocate ports
internal sealed class PortAllocator(int startPort = 8000)
{
    private int _allocatedPortStart = startPort;
    private readonly HashSet<int> _usedPorts = [];

    public int AllocatePort()
    {
        while (true)
        {
            if (!_usedPorts.Contains(_allocatedPortStart))
            {
                return _allocatedPortStart;
            }

            _allocatedPortStart++;
        }
    }

    public void AddUsedPort(int port)
    {
        _usedPorts.Add(port);
    }
}
