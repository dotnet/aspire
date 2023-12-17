// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithServiceBindingTests
{
    [Fact]
    public void ServiceBindingsWithTwoPortsSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithServiceBinding(3000, 1000, scheme: "https", name: "mybinding");
            testProgram.ServiceABuilder.WithServiceBinding(3000, 2000, scheme: "https", name: "mybinding");
        });

        Assert.Equal("Service binding with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void ServiceBindingsWithSinglePortSameNameThrows()
    {
        var ex = Assert.Throws<DistributedApplicationException>(() =>
        {
            var testProgram = CreateTestProgram();
            testProgram.ServiceABuilder.WithServiceBinding(1000, scheme: "https", name: "mybinding");
            testProgram.ServiceABuilder.WithServiceBinding(2000, scheme: "https", name: "mybinding");
        });

        Assert.Equal("Service binding with name 'mybinding' already exists", ex.Message);
    }

    [Fact]
    public void CanAddServiceBindingWithContainerPortAndEnv()
    {
        var testProgram = CreateTestProgram();
        testProgram.AppBuilder.AddExecutable("foo", "foo", ".")
                              .WithServiceBinding(containerPort: 3001, scheme: "http", name: "mybinding", env: "PORT");

        var app = testProgram.Build();

        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var exeResources = appModel.GetExecutableResources();

        var resource = Assert.Single(exeResources);
        Assert.Equal("foo", resource.Name);
        var serviceBindings = resource.Annotations.OfType<ServiceBindingAnnotation>().ToArray();
        Assert.Single(serviceBindings);
        Assert.Equal("mybinding", serviceBindings[0].Name);
        Assert.Equal(3001, serviceBindings[0].ContainerPort);
        Assert.Equal("http", serviceBindings[0].UriScheme);
        Assert.Equal("PORT", serviceBindings[0].EnvironmentVariable);
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithServiceBindingTests>(args);

}
