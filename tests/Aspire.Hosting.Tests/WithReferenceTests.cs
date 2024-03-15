// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Utils;
using Xunit;

namespace Aspire.Hosting.Tests;

public class WithReferenceTests
{
    [Theory]
    [InlineData("mybinding")]
    [InlineData("MYbinding")]
    public async Task ResourceWithSingleEndpointProducesSimplifiedEnvironmentVariables(string endpointName)
    {
        using var testProgram = CreateTestProgram();

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 2000, "mybinding");
        testProgram.ServiceABuilder.WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        // Get the service provider.
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint(endpointName));
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mybinding__0" && kvp.Value == "https://localhost:2000");
    }

    [Fact]
    public async Task ResourceWithConflictingEndpointsProducesFullyScopedEnvironmentVariables()
    {
        using var testProgram = CreateTestProgram();

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 2000, "mybinding");
        testProgram.ServiceABuilder.WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        // Create a binding and its matching annotation (simulating DCP behavior) - HOWEVER
        // this binding conflicts with the earlier because they have the same scheme.
        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 3000, "myconflictingbinding");
        testProgram.ServiceABuilder.WithEndpoint("myconflictingbinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("mybinding"));
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("myconflictingbinding"));

        // Get the service provider.
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(2, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mybinding__0" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__myconflictingbinding__0" && kvp.Value == "https://localhost:3000");
    }

    [Fact]
    public async Task ResourceWithNonConflictingEndpointsProducesAllVariantsOfEnvironmentVariables()
    {
        using var testProgram = CreateTestProgram();

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 2000, "mybinding");
        testProgram.ServiceABuilder.WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        // Create a binding and its matching annotation (simulating DCP behavior) - not
        // conflicting because the scheme is different to the first binding.
        testProgram.ServiceABuilder.WithHttpEndpoint(1000, 3000, "mynonconflictingbinding");
        testProgram.ServiceABuilder.WithEndpoint("mynonconflictingbinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));

        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("mybinding"));
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder.GetEndpoint("mynonconflictingbinding"));

        // Get the service provider.
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(2, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mybinding__0" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mynonconflictingbinding__0" && kvp.Value == "http://localhost:3000");
    }

    [Fact]
    public async Task ResourceWithConflictingEndpointsProducesAllEnvironmentVariables()
    {
        using var testProgram = CreateTestProgram();

        // Create a binding and its matching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 2000, "mybinding");
        testProgram.ServiceABuilder.WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 3000, "mybinding2");
        testProgram.ServiceABuilder.WithEndpoint("mybinding2", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));

        // The launch profile adds an "http" endpoint
        testProgram.ServiceABuilder.WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 4000));

        // Get the service provider.
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder);
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(3, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mybinding__0" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mybinding2__0" && kvp.Value == "https://localhost:3000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__http__0" && kvp.Value == "http://localhost:4000");
    }

    [Fact]
    public async Task ResourceWithEndpointsProducesAllEnvironmentVariables()
    {
        using var testProgram = CreateTestProgram();

        // Create a binding and its metching annotation (simulating DCP behavior)
        testProgram.ServiceABuilder.WithHttpsEndpoint(1000, 2000, "mybinding");
        testProgram.ServiceABuilder.WithEndpoint("mybinding", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 2000));

        testProgram.ServiceABuilder.WithHttpEndpoint(1000, 3000, "mybinding2");
        testProgram.ServiceABuilder.WithEndpoint("mybinding2", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 3000));

        // The launch profile adds an "http" endpoint
        testProgram.ServiceABuilder.WithEndpoint("http", e => e.AllocatedEndpoint = new AllocatedEndpoint(e, "localhost", 4000));

        // Get the service provider.
        testProgram.ServiceBBuilder.WithReference(testProgram.ServiceABuilder);
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(3, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mybinding__0" && kvp.Value == "https://localhost:2000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__mybinding2__0" && kvp.Value == "http://localhost:3000");
        Assert.Contains(config, kvp => kvp.Key == "services__servicea__http__0" && kvp.Value == "http://localhost:4000");
    }

    [Fact]
    public async Task ConnectionStringResourceThrowsWhenMissingConnectionString()
    {
        using var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource"));
        testProgram.ServiceBBuilder.WithReference(resource, optional: false);
        testProgram.Build();

        // Call environment variable callbacks.
        await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);
        });
    }

    [Fact]
    public async Task ConnectionStringResourceOptionalWithMissingConnectionString()
    {
        using var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource"));
        testProgram.ServiceBBuilder.WithReference(resource, optional: true);
        testProgram.Build();

        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(0, servicesKeysCount);
    }

    [Fact]
    public async Task ParameterAsConnectionStringResourceThrowsWhenConnectionStringSectionMissing()
    {
        using var testProgram = CreateTestProgram();

        // Get the service provider.
        var missingResource = testProgram.AppBuilder.AddConnectionString("missingresource");
        testProgram.ServiceBBuilder.WithReference(missingResource);
        testProgram.Build();

        // Call environment variable callbacks.
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(async () =>
        {
            var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);
        });

        Assert.Equal("Connection string parameter resource could not be used because connection string 'missingresource' is missing.", exception.Message);
    }

    [Fact]
    public async Task ParameterAsConnectionStringResourceInjectsConnectionStringWhenPresent()
    {
        using var testProgram = CreateTestProgram();
        testProgram.AppBuilder.Configuration["ConnectionStrings:resource"] = "test connection string";

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddConnectionString("resource");
        testProgram.ServiceBBuilder.WithReference(resource);
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        Assert.Equal("test connection string", config["ConnectionStrings__resource"]);
    }

    [Fact]
    public async Task ParameterAsConnectionStringResourceInjectsExpressionWhenPublishingManifest()
    {
        using var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddConnectionString("resource");
        testProgram.ServiceBBuilder.WithReference(resource);
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource, DistributedApplicationOperation.Publish);

        Assert.Equal("{resource.connectionString}", config["ConnectionStrings__resource"]);
    }

    [Fact]
    public async Task ParameterAsConnectionStringResourceInjectsCorrectEnvWhenPublishingManifest()
    {
        using var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddConnectionString("resource", "MY_ENV");
        testProgram.ServiceBBuilder.WithReference(resource);
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource, DistributedApplicationOperation.Publish);

        Assert.Equal("{resource.connectionString}", config["MY_ENV"]);
    }

    [Fact]
    public async Task ConnectionStringResourceWithConnectionString()
    {
        using var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource")
        {
            ConnectionString = "123"
        });
        testProgram.ServiceBBuilder.WithReference(resource);
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__resource" && kvp.Value == "123");
    }

    [Fact]
    public async Task ConnectionStringResourceWithConnectionStringOverwriteName()
    {
        using var testProgram = CreateTestProgram();

        // Get the service provider.
        var resource = testProgram.AppBuilder.AddResource(new TestResource("resource")
        {
            ConnectionString = "123"
        });
        testProgram.ServiceBBuilder.WithReference(resource, connectionName: "bob");
        testProgram.Build();

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceBBuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("ConnectionStrings__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "ConnectionStrings__bob" && kvp.Value == "123");
    }

    [Fact]
    public void WithReferenceHttpRelativeUriThrowsException()
    {
        using var testProgram = CreateTestProgram();

        Assert.Throws<InvalidOperationException>(() => testProgram.ServiceABuilder.WithReference("petstore", new Uri("petstore.swagger.io", UriKind.Relative)));
    }

    [Fact]
    public void WithReferenceHttpUriThrowsException()
    {
        using var testProgram = CreateTestProgram();

        Assert.Throws<InvalidOperationException>(() => testProgram.ServiceABuilder.WithReference("petstore", new Uri("https://petstore.swagger.io/v2")));
    }

    [Fact]
    public async Task WithReferenceHttpProduceEnvironmentVariables()
    {
        using var testProgram = CreateTestProgram();

        testProgram.ServiceABuilder.WithReference("petstore", new Uri("https://petstore.swagger.io/"));

        // Call environment variable callbacks.
        var config = await EnvironmentVariableEvaluator.GetEnvironmentVariablesAsync(testProgram.ServiceABuilder.Resource);

        var servicesKeysCount = config.Keys.Count(k => k.StartsWith("services__"));
        Assert.Equal(1, servicesKeysCount);
        Assert.Contains(config, kvp => kvp.Key == "services__petstore" && kvp.Value == "https://petstore.swagger.io/");
    }

    private static TestProgram CreateTestProgram(string[]? args = null) => TestProgram.Create<WithReferenceTests>(args);

    private sealed class TestResource(string name) : Resource(name), IResourceWithConnectionString
    {
        public string? ConnectionString { get; set; }

        public ReferenceExpression ConnectionStringExpression =>
            ReferenceExpression.Create($"{ConnectionString}");
    }
}
