// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.Kusto.Tests;

public class KustoPublicApiTests
{
    [Fact]
    public void KustoResourceShouldThrowWhenNameIsNull()
    {
        // Act
        var action = () => new KustoResource(null!);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void KustoResourceShouldThrowWhenNameIsInvalid(string name)
    {
        // Act
        var action = () => new KustoResource(name);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void KustoDatabaseResourceShouldThrowWhenNameIsNull()
    {
        // Arrange
        var parentResource = new KustoResource("kusto");

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
        var parentResource = new KustoResource("kusto");

        // Act
        var action = () => new KustoDatabaseResource("kusto-db", name, parentResource);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void KustoDatabaseResourceShouldThrowWhenDatabaseNameIsNull()
    {
        // Arrange
        var parentResource = new KustoResource("kusto");

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
        var parentResource = new KustoResource("kusto");

        // Act
        var action = () => new KustoDatabaseResource("kusto-db", name, parentResource);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void KustoDatabaseResourceShouldThrowWhenKustoParentResourceIsNull()
    {
        // Arrange
        KustoResource kustoParentResource = null!;

        // Act
        var action = () => new KustoDatabaseResource("kusto-db", "db1", kustoParentResource);

        // Assert
        Assert.Throws<ArgumentNullException>(action);
    }
}
