// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Dashboard.Model;

public class ServiceEndpoint
{
    public ServiceEndpoint(string address, string serviceName)
    {
        Address = address;
        ServiceName = serviceName;
    }

    public string Address { get; init; }

    public string ServiceName { get; init; }
}

public sealed record ComponentChanged<T>(ObjectChangeType ObjectChangeType, T Component)
    where T : class;
