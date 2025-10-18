// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiPlatformSelectionTests
{
    [Xunit.Fact]
    public void UnknownPlatformIsIgnored()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0");
        var builder = Hosting.DistributedApplication.CreateBuilder();
        // Call MacCatalyst even though not targeted - this should create a warning resource
        builder.AddMauiProject("maui", csproj).WithMacCatalyst();
        using var app = builder.Build();

        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        // Verify the missing TFM warning resource is created
        var maccatalystResource = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>().SingleOrDefault(r => r.Name == "maui-maccatalyst");
        Assert.NotNull(maccatalystResource);
        
        // Verify it has the missing TFM annotation
        var missingTfmAnnotation = maccatalystResource.Annotations.OfType<Aspire.Hosting.Maui.MauiMissingTfmAnnotation>().FirstOrDefault();
        Assert.NotNull(missingTfmAnnotation);
        Assert.Equal("maccatalyst", missingTfmAnnotation.PlatformMoniker);
    }

    [Xunit.Fact]
    public void MultiplePlatforms_AllResourcesPresent()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-maccatalyst", "net10.0-android");
        var builder = Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj)
               .WithWindows()
               .WithMacCatalyst()
               .WithAndroid();
        using var app = builder.Build();
        
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var resources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>().ToList();

        var win = Assert.Single(resources, r => r.Name == "maui-windows");
        var mac = Assert.Single(resources, r => r.Name == "maui-maccatalyst");
        var and = Assert.Single(resources, r => r.Name == "maui-android");

        Assert.All(new[] { win, mac, and }, r => Assert.Contains(r.Annotations, a => a is Hosting.ApplicationModel.ExplicitStartupAnnotation));

        // Verify arguments are configured by checking for CommandLineArgsCallbackAnnotation
        // Each platform should have args callbacks for -f and the TFM
        Assert.All(new[] { win, mac, and }, r => 
        {
            var argsAnnotations = r.Annotations.OfType<Hosting.ApplicationModel.CommandLineArgsCallbackAnnotation>().ToList();
            Assert.NotEmpty(argsAnnotations);
        });
        
        // Verify MacCatalyst has the OpenArguments flag by invoking its callbacks
        var macArgsAnnotations = mac.Annotations.OfType<Hosting.ApplicationModel.CommandLineArgsCallbackAnnotation>().ToList();
        var macArgs = new List<object>();
        var contextOptions = new Hosting.DistributedApplicationExecutionContextOptions(Hosting.DistributedApplicationOperation.Publish)
        {
            ServiceProvider = app.Services
        };
        var mockContext = new Hosting.ApplicationModel.CommandLineArgsCallbackContext(macArgs)
        {
            ExecutionContext = new Hosting.DistributedApplicationExecutionContext(contextOptions)
        };
        
        foreach (var annotation in macArgsAnnotations)
        {
            annotation.Callback(mockContext);
        }
        
        Assert.Contains(macArgs, a => a is string s && s == "-f");
        Assert.Contains(macArgs, a => a is string s && s == "-p:OpenArguments=-W");
    }

    [Xunit.Fact]
    public async Task NoPlatformsConfigured_EmitsWarning()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-maccatalyst", "net10.0-android", "net10.0-ios");
        var testSink = new Microsoft.Extensions.Logging.Testing.TestSink();
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions
        {
            DisableDashboard = true
        });
        builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(Microsoft.Extensions.Logging.ILoggerProvider), new Microsoft.Extensions.Logging.Testing.TestLoggerProvider(testSink)));
        builder.AddMauiProject("maui", csproj); // Intentionally no platform selection
        using var app = builder.Build();

        // Simulate the BeforeStartEvent to trigger the real subscription registered in MauiProjectBuilder without starting orchestrator services.
        var sp = app.Services;
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(sp);
        var evt = new Hosting.ApplicationModel.BeforeStartEvent(sp, model);
        // Use builder.Eventing (not the app) because StartAsync (which wires services + publishes) isn't invoked.
        await builder.Eventing.PublishAsync(evt);

        var writes = testSink.Writes.ToArray();
        var modelResources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>().ToList();

        if (OperatingSystem.IsWindows())
        {
            // On Windows, auto-detection adds windows and android platforms
            var warning = writes.SingleOrDefault(w => w.LogLevel == Microsoft.Extensions.Logging.LogLevel.Warning && (w.Message?.Contains("Auto-detected .NET MAUI platform") ?? false));
            Assert.NotNull(warning);
            Assert.Contains("windows", warning!.Message);
            Assert.Contains("android", warning!.Message);
            Assert.Contains(modelResources, r => r.Name == "maui-windows");
            Assert.Contains(modelResources, r => r.Name == "maui-android");
        }
        else if (OperatingSystem.IsMacOS())
        {
            // On macOS, auto-detection adds maccatalyst, ios, and android platforms
            var warning = writes.SingleOrDefault(w => w.LogLevel == Microsoft.Extensions.Logging.LogLevel.Warning && (w.Message?.Contains("Auto-detected .NET MAUI platform") ?? false));
            Assert.NotNull(warning);
            Assert.Contains("maccatalyst", warning!.Message);
            Assert.Contains("ios", warning!.Message);
            Assert.Contains("android", warning!.Message);
            Assert.Contains(modelResources, r => r.Name == "maui-maccatalyst");
            Assert.Contains(modelResources, r => r.Name == "maui-ios");
            Assert.Contains(modelResources, r => r.Name == "maui-android");
        }
        else
        {
            // On Linux (or other unsupported OS), auto-detect doesn't add any platforms
            // Should get the "No .NET MAUI platform resources were configured" warning instead
            var warning = writes.SingleOrDefault(w => w.LogLevel == Microsoft.Extensions.Logging.LogLevel.Warning &&
                (w.Message?.Contains("No .NET MAUI platform resources were configured") ?? false));

            Assert.NotNull(warning);

            // No platform resources should be added
            var mauiPlatformResources = modelResources.Where(r => r.Name.StartsWith("maui-", StringComparison.OrdinalIgnoreCase)).ToList();
            Assert.DoesNotContain(mauiPlatformResources, r => true); // Should be empty
        }
    }

    [Xunit.Fact]
    public void ExplicitPlatform_DisablesAutoDetect()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-maccatalyst");
        var testSink = new Microsoft.Extensions.Logging.Testing.TestSink();
        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        builder.Services.Add(new Microsoft.Extensions.DependencyInjection.ServiceDescriptor(typeof(Microsoft.Extensions.Logging.ILoggerProvider), new Microsoft.Extensions.Logging.Testing.TestLoggerProvider(testSink)));
        builder.AddMauiProject("maui", csproj).WithWindows(); // Explicit selection

        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        var writes = testSink.Writes.ToArray();
        // Should not contain auto-detected warning
        Assert.DoesNotContain(writes, w => w.LogLevel == Microsoft.Extensions.Logging.LogLevel.Warning && (w.Message?.Contains("Auto-detected .NET MAUI platform") ?? false));
        // Only the explicitly added windows resource should exist (no mac auto-added since explicit selection happened already)
        Assert.Contains(model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-windows");
        Assert.DoesNotContain(model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-maccatalyst");
    }

    [Xunit.Fact]
    public void DeferredBuildAnnotationsPresent()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0");
        var builder = Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj).WithWindows();
        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var win = Assert.Single(model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-windows");
        Assert.Contains(win.Annotations, a => a is Hosting.ApplicationModel.ExplicitStartupAnnotation);
        Assert.Contains(win.Annotations, a => a is Hosting.ApplicationModel.ManifestPublishingCallbackAnnotation);
    }

    [Xunit.Fact]
    public void AutoDetectedPlatforms_PropagateServiceDiscoveryReference()
    {
        // Only meaningful on Windows or macOS where auto-detection occurs.
        if (!OperatingSystem.IsWindows() && !OperatingSystem.IsMacOS())
        {
            return; // skip silently on other OS (Linux) where auto-detect currently no-ops.
        }

        // Create a MAUI project with multiple TFMs so auto-detect will add appropriate platforms for host OS.
        string[] mauiTfms;
        if (OperatingSystem.IsWindows())
        {
            mauiTfms = ["net10.0-windows10.0.19041.0", "net10.0-android"];
        }
        else // macOS
        {
            mauiTfms = ["net10.0-maccatalyst", "net10.0-ios"];
        }

        var mauiCsproj = MauiTestHelpers.CreateProject(mauiTfms);

        // Create a simple backend project that participates in service discovery (ProjectResource implements IResourceWithServiceDiscovery).
        var backendDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), System.IO.Path.GetRandomFileName());
        System.IO.Directory.CreateDirectory(backendDir);
        var backendProj = System.IO.Path.Combine(backendDir, "backend.csproj");
        System.IO.File.WriteAllText(backendProj, "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><TargetFramework>net8.0</TargetFramework></PropertyGroup></Project>");

        var builder = Hosting.DistributedApplication.CreateBuilder(new Hosting.DistributedApplicationOptions { DisableDashboard = true });
        var backend = builder.AddProject("backend", backendProj);
        builder.AddMauiProject("maui", mauiCsproj)
               // Intentionally do not call any explicit With* so auto-detect triggers when WithReference executes.
               .WithReference(backend);

        using var app = builder.Build();
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);

        var backendResource = Assert.Single(model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "backend");
    Assert.IsAssignableFrom<Hosting.IResourceWithServiceDiscovery>(backendResource);

        // Collect platform resources added via auto-detect.
        var platformResources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>()
            .Where(r => r.Name.StartsWith("maui-", StringComparison.OrdinalIgnoreCase) && r.Name != "maui")
            .ToList();

        Assert.NotEmpty(platformResources); // Should have at least one auto-detected platform on supported host OS.

        foreach (var pr in platformResources)
        {
            // Each platform project should have a reference relationship to backend.
            var hasReferenceRelationship = pr.Annotations
                .OfType<Hosting.ApplicationModel.ResourceRelationshipAnnotation>()
                .Any(a => a.Resource == backendResource && a.Type == "Reference");

            Assert.True(hasReferenceRelationship, $"Platform resource '{pr.Name}' did not have a service discovery reference relationship to backend.");
        }
    }
}
