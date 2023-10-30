// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithReferenceTests
{
    [Fact]
    public void ResourceWithSingleServiceBindingProducesSimplifiedEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

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
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("mybinding"));
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

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
    public void ResourceWithConflictingServiceBindingsProducesFullyScopedEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

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

        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("mybinding"));
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("myconflictingbinding"));

        // Get the service provider.
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(2, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "https://_mybinding.localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://_myconflictingbinding.localhost:3000");
    }

    [Fact]
    public void ResourceWithNonConflictingServiceBindingsProducesAllVariantsOfEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

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

        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("mybinding"));
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("mynonconflictingbinding"));

        // Get the service provider.
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
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

    [Fact]
    public void ResourceWithConflictingBindingsProducesAllBindingsEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithServiceBinding(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        testProgram.ServiceABuilder.WithServiceBinding(1000, 3000, "https", "mybinding2");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding2",
            ProtocolType.Tcp,
            "localhost",
            3000,
            "https"
            ));

        // Get the service provider.
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder);
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(2, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "https://_mybinding.localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://_mybinding2.localhost:3000");
    }

    [Fact]
    public void ResourceWithBindingsProducesAllBindingsEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithServiceBinding(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        testProgram.ServiceABuilder.WithServiceBinding(1000, 3000, "http", "mybinding2");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding2",
            ProtocolType.Tcp,
            "localhost",
            3000,
            "http"
            ));

        // Get the service provider.
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder);
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(4, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "https://_mybinding.localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__2" && kvp.Value == "http://_mybinding2.localhost:3000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__3" && kvp.Value == "http://localhost:3000");
    }

    [Fact]
    public void ConnectionStringResourceThrowsWhenMissingConnectionString()
    {
        var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource"));
        testProgram.ServiceBBuilder.WithReference(resource, optional: false);
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        Assert.Throws<DistributedApplicationException>(() =>
        {
            foreach (var annotation in annotations)
            {
                annotation.Callback(context);
            }
        });
    }

    [Fact]
    public void ConnectionStringResourceOptionalWithMissingConnectionString()
    {
        var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource"));
        testProgram.ServiceBBuilder.WithReference(resource, optional: true);
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(0, servicesKeysCount);
    }

    [Fact]
    public void ConnectionStringResourceWithConnectionString()
    {
        var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource")
        {
            ConnectionString = "123"
        });
        testProgram.ServiceBBuilder.WithReference(resource);
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__resource" && kvp.Value == "123");
    }

    [Fact]
    public void ConnectionStringResourceWithConnectionStringOverwriteName()
    {
        var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource")
        {
            ConnectionString = "123"
        });
        testProgram.ServiceBBuilder.WithReference(resource, connectionName: "bob");
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__bob" && kvp.Value == "123");
    }

    [Fact]
    public void ConnectionStringResourceMissingConnectionStringFallbackToConfig()
    {
        var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource"));
        testProgram.ServiceBBuilder.WithReference(resource);
        testProgram.AppBuilder.Configuration["ConnectionStrings:resource"] = "test";
        testProgram.Build();

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceBBuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__resource" && kvp.Value == "test");
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithReferenceTests>(args);

    private sealed class TestResource(string name) : IResourceWithConnectionString
    {
        public string Name => name;

        public string? ConnectionString { get; set; }

        public ResourceMetadataCollection Annotations => throw new NotImplementedException();

        public string? GetConnectionString()
        {
            return ConnectionString;
        }
    }
}
