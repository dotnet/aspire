// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiMissingTfmTests
{
    [Xunit.Fact]
    public async Task WithAndroid_WhenProjectDoesNotTargetAndroid_CreatesWarningResource()
    {
        // Arrange: Create a test project that only targets Windows (not Android)
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0");
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        
        // Act: Request Android platform even though project doesn't target it
        builder.AddMauiProject("myapp", csproj).WithAndroid();

        using var app = builder.Build();
        var appModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        // Assert: Verify Android resource was created with missing TFM annotation
        var androidResource = appModel.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .FirstOrDefault(r => r.Name == "myapp-android");

        Assert.NotNull(androidResource);

        // Verify the annotation is present
        var missingTfmAnnotation = androidResource.Annotations.OfType<Aspire.Hosting.Maui.MauiMissingTfmAnnotation>().FirstOrDefault();
        Assert.NotNull(missingTfmAnnotation);
        Assert.Equal("android", missingTfmAnnotation.PlatformMoniker);
        Assert.Contains("net10.0-android", missingTfmAnnotation.WarningMessage);

        // Verify the resource has the correct icon
        var iconAnnotation = androidResource.Annotations.OfType<Aspire.Hosting.ApplicationModel.ResourceIconAnnotation>().FirstOrDefault();
        Assert.NotNull(iconAnnotation);
        Assert.Equal("PhoneTablet", iconAnnotation.IconName);

        // Trigger AfterResourcesCreatedEvent to ensure state snapshot is published
        var afterResourcesEvent = new Hosting.ApplicationModel.AfterResourcesCreatedEvent(app.Services, appModel);
        await builder.Eventing.PublishAsync(afterResourcesEvent);

        // The ResourceStateSnapshot with "warning" style is published via ResourceNotificationService.
        // We've verified the annotation is present which triggers the event handler.
    }

    [Xunit.Fact]
    public void WithiOS_WhenProjectDoesNotTargetIOS_CreatesWarningResource()
    {
        // Arrange: Create a test project that only targets Windows (not iOS)
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0");
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        
        // Act: Request iOS platform even though project doesn't target it
        builder.AddMauiProject("myapp", csproj).WithiOS();

        using var app = builder.Build();
        var appModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        // Assert: Verify iOS resource was created with missing TFM annotation
        var iosResource = appModel.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .FirstOrDefault(r => r.Name == "myapp-ios");

        Assert.NotNull(iosResource);

        // Verify the annotation is present
        var missingTfmAnnotation = iosResource.Annotations.OfType<Aspire.Hosting.Maui.MauiMissingTfmAnnotation>().FirstOrDefault();
        Assert.NotNull(missingTfmAnnotation);
        Assert.Equal("ios", missingTfmAnnotation.PlatformMoniker);
        Assert.Contains("net10.0-ios", missingTfmAnnotation.WarningMessage);
    }

    [Xunit.Fact]
    public void WithAndroid_WhenProjectTargetsAndroid_DoesNotCreateWarningResource()
    {
        // Arrange: Create a test project that properly targets Android
        var csproj = MauiTestHelpers.CreateProject("net10.0-android", "net10.0-windows10.0.19041.0");
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        
        // Act: Request Android platform - project has the TFM
        builder.AddMauiProject("myapp", csproj).WithAndroid();

        using var app = builder.Build();
        var appModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        // Assert: Verify Android resource was created WITHOUT missing TFM annotation
        var androidResource = appModel.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .FirstOrDefault(r => r.Name == "myapp-android");

        Assert.NotNull(androidResource);

        // Verify NO missing TFM annotation is present
        var missingTfmAnnotation = androidResource.Annotations.OfType<Aspire.Hosting.Maui.MauiMissingTfmAnnotation>().FirstOrDefault();
        Assert.Null(missingTfmAnnotation);
    }

    [Xunit.Fact]
    public async Task MissingTfmPlatform_DoesNotThrow_OnStart()
    {
        // Test that missing TFM platforms don't throw exceptions when OnBeforeResourceStarted fires.
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0");
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        builder.AddMauiProject("myapp", csproj).WithAndroid();

        using var app = builder.Build();
        var appModel = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        var androidResource = appModel.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .First(r => r.Name == "myapp-android");

        // Verify the missing TFM annotation is present
        var missingTfmAnnotation = androidResource.Annotations.OfType<Aspire.Hosting.Maui.MauiMissingTfmAnnotation>().SingleOrDefault();
        Assert.NotNull(missingTfmAnnotation);

        // Publish BeforeResourceStartedEvent: missing TFM platforms should log and return, not throw
        var evt = new Hosting.ApplicationModel.BeforeResourceStartedEvent(androidResource, app.Services);

        // This should NOT throw an exception - the platform should silently skip starting
        await builder.Eventing.PublishAsync(evt);

        // If we reach here without exception, test passes
        Assert.True(true, "Missing TFM platform did not throw during start attempt");
    }
}
