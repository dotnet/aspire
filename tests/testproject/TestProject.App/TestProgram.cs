// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using TestProject.App.Projects;

public class TestProgram
{
    public TestProgram(string[] args)
    {
        AppBuilder = DistributedApplication.CreateBuilder(args);
        ServiceABuilder = AppBuilder.AddProject<ServiceA>();
        ServiceBBuilder = AppBuilder.AddProject<ServiceB>();
        ServiceCBuilder = AppBuilder.AddProject<ServiceC>();
    }

    public IDistributedApplicationBuilder AppBuilder { get; private set; }
    public IDistributedApplicationComponentBuilder<ProjectComponent> ServiceABuilder { get; private set; }
    public IDistributedApplicationComponentBuilder<ProjectComponent> ServiceBBuilder { get; private set; }
    public IDistributedApplicationComponentBuilder<ProjectComponent> ServiceCBuilder { get; private set; }
    public DistributedApplication? App { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        App = AppBuilder.Build();
        await App.RunAsync(cancellationToken);
    }

    public void Build()
    {
        if (App == null)
        {
            App = AppBuilder.Build();
        }
    }
    public void Run()
    {
        Build();
        App!.Run();
    }
}

