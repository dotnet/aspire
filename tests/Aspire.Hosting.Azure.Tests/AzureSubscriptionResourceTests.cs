// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using static Aspire.Hosting.Utils.AzureManifestUtils;

namespace Aspire.Hosting.Azure.Tests;

public class AzureSubscriptionResourceTests
{
    [Fact]
    public async Task AddAzureSubscription_GeneratesExpectedResourcesAndBicep()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        builder.AddAzureSubscription("mysubscription");

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);

        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Act
        var resource = Assert.Single(model.Resources.OfType<AzureSubscriptionResource>());

        var (_, bicep) = await GetManifestWithBicep(resource);

        await Verify(bicep, extension: "bicep");
    }

    [Fact]
    public void AddAzureSubscription_CreatesResourceWithCorrectName()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Act
        var subscription = builder.AddAzureSubscription("mysubscription");

        // Assert
        Assert.Equal("mysubscription", subscription.Resource.Name);
    }

    [Fact]
    public void AddAzureSubscription_ExposesSubscriptionId()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Act
        var subscription = builder.AddAzureSubscription("mysubscription");

        // Assert
        Assert.NotNull(subscription.Resource.SubscriptionId);
        Assert.Equal("subscriptionId", subscription.Resource.SubscriptionId.Name);
    }

    [Fact]
    public void AddAzureSubscription_ThrowsArgumentNullException_WhenBuilderIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            AzureSubscriptionResourceExtensions.AddAzureSubscription(null!, "mysubscription"));
    }

    [Fact]
    public void AddAzureSubscription_ThrowsArgumentException_WhenNameIsNull()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddAzureSubscription(null!));
    }

    [Fact]
    public void AddAzureSubscription_ThrowsArgumentException_WhenNameIsEmpty()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => builder.AddAzureSubscription(string.Empty));
    }

    [Fact]
    public void AddAzureSubscription_InRunMode_DoesNotAddToResources()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run);

        // Act
        builder.AddAzureSubscription("mysubscription");

        using var app = builder.Build();
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert - In run mode, the resource should not be added to the model
        Assert.Empty(model.Resources.OfType<AzureSubscriptionResource>());
    }

    [Fact]
    public async Task AddAzureSubscription_InPublishMode_AddsToResources()
    {
        // Arrange
        var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Publish);

        // Act
        builder.AddAzureSubscription("mysubscription");

        using var app = builder.Build();
        await ExecuteBeforeStartHooksAsync(app, default);
        var model = app.Services.GetRequiredService<DistributedApplicationModel>();

        // Assert - In publish mode, the resource should be added to the model
        var resource = Assert.Single(model.Resources.OfType<AzureSubscriptionResource>());
        Assert.Equal("mysubscription", resource.Name);
    }
}
