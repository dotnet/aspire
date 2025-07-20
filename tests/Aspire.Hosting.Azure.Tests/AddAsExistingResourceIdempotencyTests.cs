// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Azure;

namespace Aspire.Hosting.Azure.Tests;

public class AddAsExistingResourceIdempotencyTests
{
    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureStorageResource()
    {
        // Arrange
        var storageResource = new AzureStorageResource("test-storage", _ => { });
        var infrastructure = new AzureResourceInfrastructure(storageResource, "test-storage");

        // Act - Call AddAsExistingResource twice
        var firstResult = storageResource.AddAsExistingResource(infrastructure);
        var secondResult = storageResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureKeyVaultResource()
    {
        // Arrange
        var keyVaultResource = new AzureKeyVaultResource("test-keyvault", _ => { });
        var infrastructure = new AzureResourceInfrastructure(keyVaultResource, "test-keyvault");

        // Act - Call AddAsExistingResource twice
        var firstResult = keyVaultResource.AddAsExistingResource(infrastructure);
        var secondResult = keyVaultResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }

    [Fact]
    public void AddAsExistingResource_ShouldBeIdempotent_ForAzureRedisCacheResource()
    {
        // Arrange
        var redisResource = new AzureRedisCacheResource("test-redis", _ => { });
        var infrastructure = new AzureResourceInfrastructure(redisResource, "test-redis");

        // Act - Call AddAsExistingResource twice
        var firstResult = redisResource.AddAsExistingResource(infrastructure);
        var secondResult = redisResource.AddAsExistingResource(infrastructure);

        // Assert - Both calls should return the same resource instance, not duplicates
        Assert.Same(firstResult, secondResult);
    }
}