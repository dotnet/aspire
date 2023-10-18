// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

public class TestProgram
{
    public TestProgram(string[] args)
    {
        AppBuilder = DistributedApplication.CreateBuilder(args);
        ServiceABuilder = AppBuilder.AddProject<Projects.ServiceA>("servicea");
        ServiceBBuilder = AppBuilder.AddProject<Projects.ServiceB>("serviceb");
        ServiceCBuilder = AppBuilder.AddProject<Projects.ServiceC>("servicec");
    }

    public IDistributedApplicationBuilder AppBuilder { get; private set; }
    public IDistributedApplicationResourceBuilder<ProjectResource> ServiceABuilder { get; private set; }
    public IDistributedApplicationResourceBuilder<ProjectResource> ServiceBBuilder { get; private set; }
    public IDistributedApplicationResourceBuilder<ProjectResource> ServiceCBuilder { get; private set; }
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

