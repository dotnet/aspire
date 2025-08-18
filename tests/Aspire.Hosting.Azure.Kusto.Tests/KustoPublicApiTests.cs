// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Kusto.Tests;

public class KustoPublicApiTests
{
    [Fact]
    public void AzureKustoClusterResourceShouldThrowWhenNameIsNull()
    {
        // Act
        var action = () => new AzureKustoClusterResource(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AzureKustoClusterResourceShouldThrowWhenNameIsInvalid(string name)
    {
        // Act
        var action = () => new AzureKustoClusterResource(name);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AzureKustoClusterResourceShouldReturnValidReferenceExpression()
    {
        // Arrange
        var resource = new AzureKustoClusterResource("test-kusto");

        // Act
        var connectionStringExpression = resource.ConnectionStringExpression;

        // Assert
        Assert.Equal("{test-kusto.bindings.http.scheme}://{test-kusto.bindings.http.host}:{test-kusto.bindings.http.port}", connectionStringExpression.ValueExpression);
    }

    [Fact]
    public void KustoDatabaseResourceShouldThrowWhenNameIsNull()
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto");

        // Act
        var action = () => new KustoDatabaseResource(null!, "db", parentResource);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void KustoDatabaseResourceShouldThrowWhenNameIsInvalid(string name)
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto");

        // Act
        var action = () => new KustoDatabaseResource("kusto-db", name, parentResource);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void KustoDatabaseResourceShouldThrowWhenDatabaseNameIsNull()
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto");

        // Act
        var action = () => new KustoDatabaseResource("kusto-db", null!, parentResource);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void KustoDatabaseResourceShouldThrowWhenDatabaseNameIsInvalid(string name)
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto");

        // Act
        var action = () => new KustoDatabaseResource("kusto-db", name, parentResource);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void KustoDatabaseResourceShouldThrowWhenKustoParentResourceIsNull()
    {
        // Arrange
        AzureKustoClusterResource kustoParentResource = null!;

        // Act
        var action = () => new KustoDatabaseResource("kusto-db", "db1", kustoParentResource);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void KustoDatabaseResourceShouldReturnValidReferenceExpression()
    {
        // Arrange
        var resource = new KustoDatabaseResource("kusto-db", "myDatabase", new AzureKustoClusterResource("kusto"));

        // Act
        var connectionStringExpression = resource.ConnectionStringExpression;

        // Assert
        Assert.Equal("{kusto.connectionString};Initial Catalog=myDatabase", connectionStringExpression.ValueExpression);
    }

    [Fact]
    public void KustoEmulatorResourceShouldThrowWhenInnerResourceIsNull()
    {
        // Act
        var action = () => new KustoEmulatorResource(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void KustoEmulatorResourceShouldReturnValidReferenceExpression()
    {
        // Arrange
        var resource = new KustoEmulatorResource(new AzureKustoClusterResource("test-kusto"));

        // Act
        var connectionStringExpression = resource.ConnectionStringExpression;

        // Assert
        Assert.Equal("{test-kusto.bindings.http.scheme}://{test-kusto.bindings.http.host}:{test-kusto.bindings.http.port}", connectionStringExpression.ValueExpression);
    }
}
