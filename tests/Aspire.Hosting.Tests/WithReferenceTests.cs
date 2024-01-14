// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithReferenceTests
{
    [Fact]
    public void ResourceWithSingleEndpointProducesSimplifiedEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithEndpoint(1000, 2000, "https", "mybinding");
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
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "mybinding://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://localhost:2000");
    }

    [Fact]
    public void ResourceWithConflictingEndpointsProducesFullyScopedEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithEndpoint(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        // Create a binding and its matching annotation (simulating DCP behavior) - HOWEVER
        // this binding conflicts with the earlier because they have the same scheme.
        testProgram.ServiceABuilder.WithEndpoint(1000, 3000, "https", "myconflictingbinding");
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
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "mybinding://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "myconflictingbinding://localhost:3000");
    }

    [Fact]
    public void ResourceWithNonConflictingEndpointsProducesAllVariantsOfEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithEndpoint(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        // Create a binding and its matching annotation (simulating DCP behavior) - not
        // conflicting because the scheme is different to the first binding.
        testProgram.ServiceABuilder.WithEndpoint(1000, 3000, "http", "mynonconflictingbinding");
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
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "mybinding://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__2" && kvp.Value == "mynonconflictingbinding://localhost:3000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__3" && kvp.Value == "http://localhost:3000");
    }

    [Fact]
    public void ResourceWithConflictingEndpointsProducesAllEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithEndpoint(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        testProgram.ServiceABuilder.WithEndpoint(1000, 3000, "https", "mybinding2");
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
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "mybinding://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "mybinding2://localhost:3000");
    }

    [Fact]
    public void ResourceWithEndpointsProducesAllEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithEndpoint(1000, 2000, "https", "mybinding");
        testProgram.ServiceABuilder.WithAnnotation(
            new AllocatedEndpointAnnotation("mybinding",
            ProtocolType.Tcp,
            "localhost",
            2000,
            "https"
            ));

        testProgram.ServiceABuilder.WithEndpoint(1000, 3000, "http", "mybinding2");
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
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__0" && kvp.Value == "mybinding://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__1" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__2" && kvp.Value == "mybinding2://localhost:3000");
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

    [Fact]
    public void WithReferenceHttpRelativeUriThrowsException()
    {
        var testProgram = CreateTestProgram();

        Assert.Throws<InvalidOperationException>(() => testProgram.ServiceABuilder.WithReference("petstore", new Uri("petstore.swagger.io", UriKind.Relative)));
    }

    [Fact]
    public void WithReferenceHttpUriThrowsException()
    {
        var testProgram = CreateTestProgram();

        Assert.Throws<InvalidOperationException>(() => testProgram.ServiceABuilder.WithReference("petstore", new Uri("https://petstore.swagger.io/v2")));
    }

    [Fact]
    public void WithReferenceHttpProduceEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        testProgram.ServiceABuilder.WithReference("petstore", new Uri("https://petstore.swagger.io/"));

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__petstore" && kvp.Value == "https://petstore.swagger.io/");
    }

    [Fact]
    public void WithReferenceToConnectionStringEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        var connectionString = new ConnectionString("petstoredb", "host=mydb;port=1234");
        testProgram.ServiceABuilder.WithReference(connectionString);

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__petstoredb" && kvp.Value == "host=mydb;port=1234");
    }

    [Fact]
    public void WithReferenceToConnectionStringNameOnlyEnvironmentVariables()
    {
        var testProgram = CreateTestProgram();

        testProgram.AppBuilder.Configuration["ConnectionStrings:petstoredb"] = "host=mydb;port=1234";

        var connectionString = new ConnectionString("petstoredb");
        testProgram.ServiceABuilder.WithReference(connectionString);

        // Call environment variable callbacks.
        var annotations = testProgram.ServiceABuilder.Resource.Annotations.OfType<EnvironmentCallbackAnnotation>();

        var config = new Dictionary<string, string>();
        var context = new EnvironmentCallbackContext("dcp", config);

        foreach (var annotation in annotations)
        {
            annotation.Callback(context);
        }

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__petstoredb" && kvp.Value == "host=mydb;port=1234");
    }

    [Fact]
    public void WithRefernceConnectionStringThrowsWhenMissingConnectionString()
    {
        var testProgram = CreateTestProgram();

        // Get the service provider.
        testProgram.ServiceBBuilder.WithReference(new ConnectionString("missing"));
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
