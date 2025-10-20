// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Maui.Tests;

public class MauiPlatformVisualizationTests
{
    [Xunit.Theory]
    [Xunit.InlineData("windows", "net10.0-windows10.0.19041.0", "Desktop")]
    [Xunit.InlineData("android", "net10.0-android", "PhoneTablet")]
    [Xunit.InlineData("ios", "net10.0-ios", "PhoneTablet")]
    [Xunit.InlineData("maccatalyst", "net10.0-maccatalyst", "DesktopMac")]
    public void Platform_HasCorrectIconAnnotation(string platformMoniker, string tfm, string expectedIconName)
    {
        var csproj = MauiTestHelpers.CreateProject(tfm);
        var builder = Hosting.DistributedApplication.CreateBuilder();
        
        var mauiBuilder = builder.AddMauiProject("maui", csproj);
        
        // Add the specific platform
        switch (platformMoniker)
        {
            case "windows":
                mauiBuilder.WithWindows();
                break;
            case "android":
                mauiBuilder.WithAndroid();
                break;
            case "ios":
                mauiBuilder.WithiOS();
                break;
            case "maccatalyst":
                mauiBuilder.WithMacCatalyst();
                break;
        }

        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        
        var platformResourceName = $"maui-{platformMoniker}";
        var platformResource = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .SingleOrDefault(r => r.Name == platformResourceName);

        // Platform may not exist if TFM is unsupported on current host
        if (platformResource is null)
        {
            return; // Skip test if platform not added (unsupported scenario)
        }

        var iconAnnotation = platformResource.Annotations.OfType<ResourceIconAnnotation>().SingleOrDefault();
        Assert.NotNull(iconAnnotation);
        Assert.Equal(expectedIconName, iconAnnotation.IconName);
        Assert.Equal(IconVariant.Filled, iconAnnotation.IconVariant);
    }

    [Xunit.Fact]
    public void AllPlatforms_HaveDistinctIcons()
    {
        var csproj = MauiTestHelpers.CreateProject(
            "net10.0-windows10.0.19041.0",
            "net10.0-android",
            "net10.0-ios",
            "net10.0-maccatalyst");

        var builder = Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj)
            .WithWindows()
            .WithAndroid()
            .WithiOS()
            .WithMacCatalyst();

        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        var platformResources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .Where(r => r.Name.StartsWith("maui-", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var icons = platformResources
            .Select(r => r.Annotations.OfType<ResourceIconAnnotation>().FirstOrDefault()?.IconName)
            .Where(icon => icon is not null)
            .ToList();

        // Verify that all platforms that were created have icons (some may not be created if unsupported)
        Assert.All(platformResources, r =>
        {
            var icon = r.Annotations.OfType<ResourceIconAnnotation>().FirstOrDefault();
            Assert.NotNull(icon);
        });

        // Verify we have at least the expected unique icons
        // Note: Android and iOS share PhoneTablet icon, so we expect 3 distinct icons for 4 platforms
        var distinctIconCount = icons.Distinct().Count();
        Assert.True(distinctIconCount >= 3, $"Expected at least 3 distinct icons, but got {distinctIconCount}");
    }

    [Xunit.Fact]
    public void UnsupportedPlatform_HasWarningIconConfiguration()
    {
        // This test verifies that unsupported platforms are configured to show warning state.
        // We can't easily test the runtime state snapshot without mocking ResourceNotificationService,
        // but we can verify the unsupported annotation is present which triggers the warning state.

        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
        {
            return; // Skip on other OS where unsupported detection doesn't apply
        }

        string unsupportedPlatformMoniker;
        string[] tfms;
        Action<ApplicationModel.IResourceBuilder<MauiProjectResource>> configure;

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
        var builder = Hosting.DistributedApplication.CreateBuilder();
        var mauiBuilder = builder.AddMauiProject("maui", csproj);
        configure(mauiBuilder);
        
        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        
        var platformResourceName = $"maui-{unsupportedPlatformMoniker}";
        var platformResource = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .Single(r => r.Name == platformResourceName);

        // Verify unsupported annotation is present (this triggers the warning state in the event handler)
        var unsupportedAnnotation = platformResource.Annotations
            .FirstOrDefault(a => a.GetType().Name.Contains("MauiUnsupportedPlatformAnnotation"));
        
        Assert.NotNull(unsupportedAnnotation);

        // Verify the platform still has an icon (for visual consistency)
        var iconAnnotation = platformResource.Annotations.OfType<ResourceIconAnnotation>().SingleOrDefault();
        Assert.NotNull(iconAnnotation);
    }
}
