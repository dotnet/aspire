// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithArgsTests
{
    [Fact]
    public void WithArgsAddsAnnotationToExecutableResource()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddExecutable("dotnet", "dotnet", Environment.CurrentDirectory)
            .WithArgs("--version");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var executableResources = appModel.GetExecutableResources();

        var resource = Assert.Single(executableResources);

        var annotations = resource.Annotations.OfType<ExecutableArgsCallbackAnnotation>();

        Assert.NotNull(resource.Args);
        Assert.Empty(resource.Args);

        var argsList = resource.Args.ToList();

        foreach (var annotation in annotations)
        {
            annotation.Callback(argsList);
        }

        Assert.Collection(argsList, arg => Assert.Equal("--version", arg));
    }

    [Fact]
    public void WithArgsAddsAnnotationToContainerResource()
    {
        var appBuilder = CreateBuilder();

        appBuilder.AddContainer("acontainer", "acontainer")
            .WithArgs("anArg", "another", "oneMore");

        var app = appBuilder.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var containerResources = appModel.GetContainerResources();

        var resource = Assert.Single(containerResources);

        var annotations = resource.Annotations.OfType<ExecutableArgsCallbackAnnotation>();

        var argsList = new List<string>();

        foreach (var annotation in annotations)
        {
            annotation.Callback(argsList);
        }

        Assert.Collection(argsList,
            arg => Assert.Equal("anArg", arg),
            arg => Assert.Equal("another", arg),
            arg => Assert.Equal("oneMore", arg));
    }

    private static IDistributedApplicationBuilder CreateBuilder()
    {
        var appBuilder = DistributedApplication.CreateBuilder(["--publisher", "manifest"]);
        // Block DCP from actually starting anything up as we don't need it for this test.
        appBuilder.Services.AddKeyedSingleton<IDistributedApplicationPublisher, NoopPublisher>("manifest");

        return appBuilder;
    }
}
