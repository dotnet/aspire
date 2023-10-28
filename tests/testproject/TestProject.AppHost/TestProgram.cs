// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

public class TestProgram
{
    private TestProgram(string[] args, Assembly assembly)
    {
        AppBuilder = DistributedApplication.CreateBuilder(new DistributedApplicationOptions { Args = args, DisableDashboard = true, AssemblyName = assembly.FullName });
        ServiceABuilder = AppBuilder.AddProject<Projects.ServiceA>("servicea");
        ServiceBBuilder = AppBuilder.AddProject<Projects.ServiceB>("serviceb");
        ServiceCBuilder = AppBuilder.AddProject<Projects.ServiceC>("servicec");
    }

    public static TestProgram Create<T>(string[]? args = null) => new TestProgram(args ?? [], typeof(T).Assembly);

    public IDistributedApplicationBuilder AppBuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceABuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceBBuilder { get; private set; }
    public IResourceBuilder<ProjectResource> ServiceCBuilder { get; private set; }
    public DistributedApplication? App { get; private set; }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        App = AppBuilder.Build();
        await App.RunAsync(cancellationToken);
    }

    public DistributedApplication Build()
    {
        if (App == null)
        {
            App = AppBuilder.Build();
        }
        return App;
    }
    public void Run()
    {
        Build();
        App!.Run();
    }
}

