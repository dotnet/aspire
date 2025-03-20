// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Microsoft.Azure.Cosmos.Tests;

public class AspireMicrosoftAzureCosmosExtensionsTests
{
    [Theory]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;Database=db;", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;Database=db;Container=mycontainer;", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db;DisableServerCertificateValidation=True", "https://localhost:8081/")]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=db;Container=mycontainer", "https://localhost:8081/")]
    [InlineData("https://example1.documents.azure.com:443", "https://example1.documents.azure.com/")]
    public void AddAzureCosmosClient_EnsuresConnectionStringIsCorrect(string connectionString, string expectedEndpoint)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureCosmosClient("cosmos");

        using var host = builder.Build();
        var client = host.Services.GetRequiredService<CosmosClient>();

        Assert.Equal(expectedEndpoint, client.Endpoint.ToString());
    }

    [Fact]
    public void AddAzureCosmosClient_FailsWithError()
    {
        var e = Assert.Throws<ArgumentException>(() =>
            AddAzureCosmosClient_EnsuresConnectionStringIsCorrect("this=isnt;a=valid;cosmos=connectionstring", string.Empty));

        Assert.Contains("missing", e.Message);
        Assert.Contains("AccountEndpoint", e.Message);
    }

    [Theory]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=testdb;Container=mycontainers")]
    [InlineData("AccountEndpoint=https://localhost:8081;AccountKey=fake;Database=testdb;")]
    public void AddAzureCosmosClient_WorksWithChildConnectionStrings(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureCosmosClient("cosmos");

        using var host = builder.Build();
        var client = host.Services.GetService<CosmosClient>();

        Assert.NotNull(client);
        Assert.Equal(expectedEndpoint, client.Endpoint.ToString());
    }

    [Fact]
    public void AddAzureCosmosDatabase_RegistersDatabaseService()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var databaseName = "testdb";
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;Database={databaseName};");

        builder.AddAzureCosmosDatabase("cosmos");

        using var host = builder.Build();
        var database = host.Services.GetRequiredService<Database>();
        var client = host.Services.GetService<CosmosClient>();

        Assert.Null(client);
        Assert.NotNull(database);
        Assert.Equal(databaseName, database.Id);
        Assert.Equal(expectedEndpoint, database.Client.Endpoint.ToString());
    }

    [Fact]
    public void AddAzureCosmosContainer_RegistersContainerService()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var databaseName = "testdb";
        var containerName = "testcontainer";
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;Database={databaseName};Container={containerName};");

        builder.AddAzureCosmosContainer("cosmos");

        using var host = builder.Build();
        var container = host.Services.GetRequiredService<Container>();
        var database = host.Services.GetService<Database>();
        var client = host.Services.GetService<CosmosClient>();

        Assert.NotNull(container);
        Assert.Null(client);
        Assert.Null(database);
        Assert.Equal(containerName, container.Id);
        Assert.Equal(expectedEndpoint, container.Database.Client.Endpoint.ToString());
    }

    [Fact]
    public void AddKeyedAzureCosmosDatabase_RegistersDatabaseService()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serviceKey = "cosmos-key";
        var databaseName = "testdb";
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;Database={databaseName};", serviceKey);

        builder.AddKeyedAzureCosmosDatabase(serviceKey);

        using var host = builder.Build();
        var database = host.Services.GetRequiredKeyedService<Database>(serviceKey);
        var client = host.Services.GetKeyedService<CosmosClient>(serviceKey);

        Assert.Null(client);
        Assert.NotNull(database);
        Assert.Equal(databaseName, database.Id);
        Assert.Equal(expectedEndpoint, database.Client.Endpoint.ToString());
    }

    [Fact]
    public void AddKeyedAzureCosmosContainer_RegistersContainerService()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serviceKey = "cosmos-key";
        var databaseName = "testdb";
        var containerName = "testcontainer";
        var expectedEndpoint = "https://localhost:8081/";

        PopulateConfiguration(builder.Configuration, $"AccountEndpoint={expectedEndpoint};AccountKey=fake;Database={databaseName};Container={containerName}", serviceKey);

        builder.AddKeyedAzureCosmosContainer(serviceKey);

        using var host = builder.Build();
        var container = host.Services.GetRequiredKeyedService<Container>(serviceKey);
        var database = host.Services.GetKeyedService<Database>(serviceKey);
        var client = host.Services.GetKeyedService<CosmosClient>(serviceKey);

        Assert.NotNull(container);
        Assert.Null(client);
        Assert.Null(database);
        Assert.Equal(containerName, container.Id);
        Assert.Equal(expectedEndpoint, container.Database.Client.Endpoint.ToString());
    }

    [Theory]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake;Container=containerName")]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake;Database=databaseName")]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake")]
    public void AddAzureCosmosContainer_ThrowsForInvalidConnectionString(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serviceKey = "cosmos-key";

        PopulateConfiguration(builder.Configuration, connectionString);
        builder.AddAzureCosmosContainer(serviceKey);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<Container>);
        Assert.Equal("The connection string 'cosmos-key' does not exist or is missing the container name or database name.", exception.Message);
    }

    [Theory]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake;Container=containerName")]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake;Database=databaseName")]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake")]
    public void AddKeyedAzureCosmosContainer_ThrowsForInvalidConnectionString(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serviceKey = "cosmos-key";

        PopulateConfiguration(builder.Configuration, connectionString, serviceKey);
        builder.AddKeyedAzureCosmosContainer(serviceKey);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => host.Services.GetKeyedService<Container>(serviceKey));
        Assert.Equal("The connection string 'cosmos-key' does not exist or is missing the container name or database name.", exception.Message);
    }

    [Theory]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake;Container=containerName")]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake")]
    public void AddAzureCosmosDatabase_ThrowsForInvalidConnectionString(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serviceKey = "cosmos-key";

        PopulateConfiguration(builder.Configuration, connectionString);
        builder.AddAzureCosmosDatabase(serviceKey);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<Database>);
        Assert.Equal("The connection string 'cosmos-key' does not exist or is missing the database name.", exception.Message);
    }

    [Theory]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake;Container=containerName")]
    [InlineData("AccountEndpoint=https://localhost:8081/;AccountKey=fake")]
    public void AddKeyedAzureCosmosDatabase_ThrowsForInvalidConnectionString(string connectionString)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var serviceKey = "cosmos-key";

        PopulateConfiguration(builder.Configuration, connectionString, serviceKey);
        builder.AddKeyedAzureCosmosDatabase(serviceKey);
        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(() => host.Services.GetRequiredKeyedService<Database>(serviceKey));
        Assert.Contains("The connection string 'cosmos-key' does not exist or is missing the database name.", exception.Message);
    }

    private static void PopulateConfiguration(ConfigurationManager configuration, string connectionString, string? key = null) =>
        configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>($"ConnectionStrings:{key ?? "cosmos"}", connectionString)
        ]);
}
