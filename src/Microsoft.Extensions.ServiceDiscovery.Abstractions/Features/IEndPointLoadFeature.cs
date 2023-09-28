// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

public interface IEndPointLoadFeature
{
    // CurrentLoad is some comparable measure of load (queue length, concurrent requests, etc)
    public double CurrentLoad { get; }
}

