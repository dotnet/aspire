// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiUnsupportedPlatformTests
{
    [Xunit.Fact]
    public async Task UnsupportedPlatformStart_Throws()
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
        Action<Aspire.Hosting.Maui.MauiProjectBuilder> configure;

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

        // Ensure the unsupported annotation is present (defensive assertion of setup correctness) by type name match only (annotation class is private).
        Assert.Contains(platformResource.Annotations, a => a.GetType().Name.Contains("MauiUnsupportedPlatformAnnotation"));

        // Publish BeforeResourceStartedEvent: unsupported check now runs before startup phase gating and throws.
        var evt = new Hosting.ApplicationModel.BeforeResourceStartedEvent(platformResource, app.Services);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => builder.Eventing.PublishAsync(evt));
        Assert.Contains("cannot be started on this host", ex.Message);
    }
}
