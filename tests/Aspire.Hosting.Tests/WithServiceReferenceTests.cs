// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithServiceReferenceTests
{
    [Fact]
    public void ComponentWithSingleServiceBindingProducesSimplifiedEnvironmentVariables()
    {
        var testProgram = new TestProgram([]);

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithServiceBinding(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        // Get the service provider.
        testProgram.ServiceBBuilder.WithServiceReference(testProgram.ServiceABuilder, "mybinding");
        testProgram.AppBuilder.Build();
        var sp = testProgram.App?.Services.GetRequiredService<IServiceProvider>();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Component.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext(sp!, config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(2, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "https://_mybinding.localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://localhost:2000");
    }

    [Fact]
    public void ComponentWithConflictingServiceBindingsProducesFullyScopedEnvironmentVariables()
    {
        var testProgram = new TestProgram([]);

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithServiceBinding(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        // Create a binding and its matching annotation (simulating DCP behavior) - HOWEVER
        // this binding conflicts with the earlier because they have the same scheme.
        testProgram.ServiceABuilder.WithServiceBinding(1000, 3000, "https", "myconflictingbinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("myconflictingbinding",
            ProtocolType.Tcp,
            "localhost",
            3000,
            "https"
            ));

        testProgram.ServiceBBuilder.WithServiceReference(testProgram.ServiceABuilder, "mybinding");
        testProgram.ServiceBBuilder.WithServiceReference(testProgram.ServiceABuilder, "myconflictingbinding");

        // Get the service provider.
        testProgram.ServiceBBuilder.WithServiceReference(testProgram.ServiceABuilder, "mybinding");
        testProgram.AppBuilder.Build();
        var sp = testProgram.App?.Services.GetRequiredService<IServiceProvider>();

        // Call environment variable callbacks.
        var annoations = testProgram.ServiceBBuilder.Component.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext(sp!, config);

        foreach (var annotation in annoations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(2, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "https://_mybinding.localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://_myconflictingbinding.localhost:3000");
    }

    [Fact]
    public void ComponentWithNonConflictingServiceBindingsProducesAllVariantsOfEnvironmentVariables()
    {
        var testProgram = new TestProgram([]);

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithServiceBinding(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        // Create a binding and its matching annotation (simulating DCP behavior) - not
        // conflicting because the scheme is different to the first binding.
        testProgram.ServiceABuilder.WithServiceBinding(1000, 3000, "http", "mynonconflictingbinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mynonconflictingbinding",
            ProtocolType.Tcp,
            "localhost",
            3000,
            "http"
            ));

        testProgram.ServiceBBuilder.WithServiceReference(testProgram.ServiceABuilder, "mybinding");
        testProgram.ServiceBBuilder.WithServiceReference(testProgram.ServiceABuilder, "mynonconflictingbinding");

        // Get the service provider.
        testProgram.ServiceBBuilder.WithServiceReference(testProgram.ServiceABuilder, "mybinding");
        testProgram.AppBuilder.Build();
        var sp = testProgram.App?.Services.GetRequiredService<IServiceProvider>();

        // Call environment variable callbacks.
        var annoations = testProgram.ServiceBBuilder.Component.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext(sp!, config);

        foreach (var annotation in annoations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(4, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "https://_mybinding.localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__2" && kvp.Value == "http://_mynonconflictingbinding.localhost:3000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__3" && kvp.Value == "http://localhost:3000");
    }
}
