// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Kusto.Tests;

public class KustoPublicApiTests
{
    [Fact]
    public void AzureKustoClusterResourceShouldThrowWhenNameIsNull()
    {
        // Act
        var action = () => new AzureKustoClusterResource(null!, _ => { });

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AzureKustoClusterResourceShouldThrowWhenNameIsInvalid(string name)
    {
        // Act
        var action = () => new AzureKustoClusterResource(name, _ => { });

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AzureKustoClusterResourceShouldReturnValidReferenceExpression()
    {
        // Arrange
        var resource = new AzureKustoClusterResource("test-kusto", _ => { });

        // Act
        var connectionStringExpression = resource.ConnectionStringExpression;

        // Assert
        // For Azure provisioned resources (default when directly constructed), should use cluster URI output
        Assert.Equal("{test-kusto.outputs.clusterUri}", connectionStringExpression.ValueExpression);
    }

    [Fact]
    public void AzureKustoDatabaseResourceShouldThrowWhenNameIsNull()
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto", _ => { });

        // Act
        var action = () => new AzureKustoReadWriteDatabaseResource(null!, "db", parentResource);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AzureKustoDatabaseResourceShouldThrowWhenNameIsInvalid(string name)
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto", _ => { });

        // Act
        var action = () => new AzureKustoReadWriteDatabaseResource("kusto-db", name, parentResource);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AzureKustoDatabaseResourceShouldThrowWhenDatabaseNameIsNull()
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto", _ => { });

        // Act
        var action = () => new AzureKustoReadWriteDatabaseResource("kusto-db", null!, parentResource);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AzureKustoDatabaseResourceShouldThrowWhenDatabaseNameIsInvalid(string name)
    {
        // Arrange
        var parentResource = new AzureKustoClusterResource("kusto", _ => { });

        // Act
        var action = () => new AzureKustoReadWriteDatabaseResource("kusto-db", name, parentResource);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void AzureKustoDatabaseResourceShouldThrowWhenKustoParentResourceIsNull()
    {
        // Arrange
        AzureKustoClusterResource kustoParentResource = null!;

        // Act
        var action = () => new AzureKustoReadWriteDatabaseResource("kusto-db", "db1", kustoParentResource);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Fact]
    public void AzureKustoDatabaseResourceShouldReturnValidReferenceExpression()
    {
        // Arrange
        var resource = new AzureKustoReadWriteDatabaseResource("kusto-db", "myDatabase", new AzureKustoClusterResource("kusto", _ => { }));

        // Act
        var connectionStringExpression = resource.ConnectionStringExpression;

        // Assert
        Assert.Equal("{kusto.connectionString};Initial Catalog=myDatabase", connectionStringExpression.ValueExpression);
    }

    [Fact]
    public void KustoEmulatorResourceShouldThrowWhenInnerResourceIsNull()
    {
        // Act
        var action = () => new AzureKustoEmulatorResource(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
