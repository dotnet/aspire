// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiMacCatalystTests
{
    [Xunit.Fact]
    public void AddsOpenArgumentsFlagWhenNotProvided()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-maccatalyst", "net10.0-windows10.0.19041.0");
        var builder = Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj).WithMacCatalyst();
        using var app = builder.Build();
        
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var macRes = Assert.Single(model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-maccatalyst");
        
        // Verify the CommandLineArgsCallbackAnnotation contains the OpenArguments flag
        var argsAnnotations = macRes.Annotations.OfType<Hosting.ApplicationModel.CommandLineArgsCallbackAnnotation>().ToList();
        Assert.NotEmpty(argsAnnotations);
        
        // Create a mock context to invoke the callbacks and collect the arguments
        var args = new List<object>();
        var contextOptions = new Hosting.DistributedApplicationExecutionContextOptions(Hosting.DistributedApplicationOperation.Publish)
        {
            ServiceProvider = app.Services
        };
        var mockContext = new Hosting.ApplicationModel.CommandLineArgsCallbackContext(args)
        {
            ExecutionContext = new Hosting.DistributedApplicationExecutionContext(contextOptions)
        };
        
        // Invoke all arg callbacks
        foreach (var annotation in argsAnnotations)
        {
            annotation.Callback(mockContext);
        }
        
        // Verify that -p:OpenArguments=-W was added by one of the callbacks
        Assert.Contains(args, a => a is string s && s == "-p:OpenArguments=-W");
    }
}
