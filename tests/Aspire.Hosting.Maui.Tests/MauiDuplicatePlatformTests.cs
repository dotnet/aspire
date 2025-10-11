// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiDuplicatePlatformTests
{
    [Xunit.Fact]
    public void DuplicatePlatform_LogsWarningAndIgnoresSecondCall()
    {
        // Arrange
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-android");
        var testSink = new Microsoft.Extensions.Logging.Testing.TestSink();
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions
        {
            DisableDashboard = true
        });
        builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(
            typeof(Microsoft.Extensions.Logging.ILoggerProvider), 
            new Microsoft.Extensions.Logging.Testing.TestLoggerProvider(testSink)));

        // Act - call WithWindows twice
        builder.AddMauiProject("maui", csproj)
            .WithWindows()
            .WithWindows(); // Duplicate!

        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        // Assert - only one Windows resource should exist
        var windowsResources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .Where(r => r.Name == "maui-windows")
            .ToList();

        Assert.Single(windowsResources);

        // Assert - warning was logged
        var warnings = testSink.Writes.Where(w => 
            w.LogLevel == Microsoft.Extensions.Logging.LogLevel.Warning && 
            w.Message != null &&
            w.Message.Contains("Platform") && 
            w.Message.Contains("already been added")).ToList();

        Assert.NotEmpty(warnings);
        var warning = warnings.First();
        Assert.Contains("windows", warning.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("WithWindows", warning.Message);
    }

    [Xunit.Fact]
    public void MultipleDifferentPlatforms_AllAdded()
    {
        // Arrange
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-android", "net10.0-maccatalyst");
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions
        {
            DisableDashboard = true
        });

        // Act - add three different platforms
        builder.AddMauiProject("maui", csproj)
            .WithWindows()
            .WithAndroid()
            .WithMacCatalyst();

        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        // Assert - all three resources should exist
        var resources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>().ToList();
        
        Assert.Contains(resources, r => r.Name == "maui-windows");
        Assert.Contains(resources, r => r.Name == "maui-android");
        Assert.Contains(resources, r => r.Name == "maui-maccatalyst");
        Assert.Equal(3, resources.Count);
    }

    [Xunit.Fact]
    public void DuplicateAndroid_LogsWarningAndIgnoresSecondCall()
    {
        // Arrange
        var csproj = MauiTestHelpers.CreateProject("net10.0-android");
        var testSink = new Microsoft.Extensions.Logging.Testing.TestSink();
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions
        {
            DisableDashboard = true
        });
        builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(
            typeof(Microsoft.Extensions.Logging.ILoggerProvider), 
            new Microsoft.Extensions.Logging.Testing.TestLoggerProvider(testSink)));

        // Act - call WithAndroid twice
        builder.AddMauiProject("maui", csproj)
            .WithAndroid()
            .WithAndroid("emulator-5554"); // Duplicate with different parameter!

        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        // Assert - only one Android resource should exist (first call wins)
        var androidResources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .Where(r => r.Name == "maui-android")
            .ToList();

        Assert.Single(androidResources);

        // Assert - warning was logged
        var warnings = testSink.Writes.Where(w => 
            w.LogLevel == Microsoft.Extensions.Logging.LogLevel.Warning && 
            w.Message != null &&
            w.Message.Contains("android", StringComparison.OrdinalIgnoreCase) && 
            w.Message.Contains("already been added")).ToList();

        Assert.NotEmpty(warnings);
    }
}
