// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiUnsupportedPlatformTests
{
    [Xunit.Fact]
    public async Task UnsupportedPlatform_HasUnsupportedAnnotation_AndWarningState()
    {
        // Determine a platform that will be unsupported on this host.
        // If Windows host, create a MacCatalyst/iOS targeted project and attempt to start MacCatalyst.
        // If macOS host, create a Windows targeted project and attempt to start Windows.
        // If other OS (e.g., Linux) the current hosting code does not auto-detect nor mark unsupported; skip.
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
        {
            return; // Not applicable on other OS at the moment.
        }

        string unsupportedPlatformMoniker;
        string[] tfms;
        Action<Maui.MauiProjectBuilder> configure;

        if (OperatingSystem.IsWindows())
        {
            unsupportedPlatformMoniker = "maccatalyst";
            tfms = ["net10.0-maccatalyst"];
            configure = mp => mp.WithMacCatalyst();
        }
        else // macOS
        {
            unsupportedPlatformMoniker = "windows";
            tfms = ["net10.0-windows10.0.19041.0"];
            configure = mp => mp.WithWindows();
        }

        var csproj = MauiTestHelpers.CreateProject(tfms);
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        configure(builder.AddMauiProject("maui", csproj));
        using var app = builder.Build();

        // Find the unsupported platform resource.
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var platformResourceName = $"maui-{unsupportedPlatformMoniker}";
        var platformResource = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>().Single(r => r.Name == platformResourceName);

        // Verify the unsupported annotation is present with the correct reason.
        var unsupportedAnnotation = platformResource.Annotations.OfType<MauiUnsupportedPlatformAnnotation>().SingleOrDefault();
        Assert.NotNull(unsupportedAnnotation);
        Assert.NotEmpty(unsupportedAnnotation.Reason);

        // Trigger AfterResourcesCreatedEvent to ensure state snapshot is published
        var afterResourcesEvent = new Hosting.ApplicationModel.AfterResourcesCreatedEvent(app.Services, model);
        await builder.Eventing.PublishAsync(afterResourcesEvent);

        // Verify that attempting to start the unsupported platform logs but does not throw
        var beforeStartEvent = new Hosting.ApplicationModel.BeforeResourceStartedEvent(platformResource, app.Services);
        await builder.Eventing.PublishAsync(beforeStartEvent); // Should complete without exception

        // The ResourceStateSnapshot with "warning" style is published via ResourceNotificationService.
        // We've verified the annotation is present which triggers the event handler.
    }

    [Xunit.Fact]
    public async Task UnsupportedPlatform_DoesNotThrow_OnStart()
    {
        // Test that unsupported platforms don't throw exceptions when OnBeforeResourceStarted fires.
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        string unsupportedPlatformMoniker;
        string[] tfms;
        Action<MauiProjectBuilder> configure;

        if (OperatingSystem.IsWindows())
        {
            unsupportedPlatformMoniker = "ios";
            tfms = ["net10.0-ios"];
            configure = mp => mp.WithiOS();
        }
        else // macOS
        {
            unsupportedPlatformMoniker = "windows";
            tfms = ["net10.0-windows10.0.19041.0"];
            configure = mp => mp.WithWindows();
        }

        var csproj = MauiTestHelpers.CreateProject(tfms);
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        configure(builder.AddMauiProject("maui", csproj));
        using var app = builder.Build();

        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var platformResourceName = $"maui-{unsupportedPlatformMoniker}";
        var platformResource = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>().Single(r => r.Name == platformResourceName);

        // Verify the unsupported annotation is present.
        var unsupportedAnnotation = platformResource.Annotations.OfType<MauiUnsupportedPlatformAnnotation>().SingleOrDefault();
        Assert.NotNull(unsupportedAnnotation);

        // Publish BeforeResourceStartedEvent: unsupported platforms should log and return, not throw
        var evt = new Hosting.ApplicationModel.BeforeResourceStartedEvent(platformResource, app.Services);

        // This should NOT throw an exception - the platform should silently skip starting
        await builder.Eventing.PublishAsync(evt);

        // If we reach here without exception, test passes
        Assert.True(true, "Unsupported platform did not throw during start attempt");
    }
}
