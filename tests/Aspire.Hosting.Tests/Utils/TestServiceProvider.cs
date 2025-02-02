// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel.Design;
using Aspire.Hosting.Dcp;
using Aspire.Hosting.Tests.Dcp;

namespace Aspire.Hosting.Tests.Utils;

public sealed class TestServiceProvider : IServiceProvider
{
    private readonly ServiceContainer _serviceContainer = new ServiceContainer();

    private TestServiceProvider()
    {
        _serviceContainer.AddService(typeof(IDcpDependencyCheckService), new TestDcpDependencyCheckService());
    }

    public object? GetService(Type serviceType)
    {
        return _serviceContainer.GetService(serviceType);
    }

    public static IServiceProvider Instance { get; } = new TestServiceProvider();
}
