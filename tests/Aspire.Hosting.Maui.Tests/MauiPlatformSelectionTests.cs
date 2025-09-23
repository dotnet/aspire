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
        // Call MacCatalyst even though not targeted
        builder.AddMauiProject("maui", csproj).WithMacCatalyst();
        using var app = builder.Build();

        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        Assert.DoesNotContain(model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-maccatalyst");
    }

    [Xunit.Fact]
    public async Task MultiplePlatforms_AllResourcesPresent()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-maccatalyst", "net10.0-android");
        var builder = Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj)
               .WithWindows()
               .WithMacCatalyst()
               .WithAndroid();
        using var app = builder.Build();
        // Host startup not required for inspecting platform resource annotations & args.
        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var resources = model.Resources.OfType<Hosting.ApplicationModel.ProjectResource>().ToList();

        var win = Assert.Single(resources, r => r.Name == "maui-windows");
        var mac = Assert.Single(resources, r => r.Name == "maui-maccatalyst");
        var and = Assert.Single(resources, r => r.Name == "maui-android");

        Assert.All(new[] { win, mac, and }, r => Assert.Contains(r.Annotations, a => a is Hosting.ApplicationModel.ExplicitStartupAnnotation));

        var winArgs = await Hosting.ApplicationModel.ResourceExtensions.GetArgumentValuesAsync(win, Hosting.DistributedApplicationOperation.Publish);
        var macArgs = await Hosting.ApplicationModel.ResourceExtensions.GetArgumentValuesAsync(mac, Hosting.DistributedApplicationOperation.Publish);
        var andArgs = await Hosting.ApplicationModel.ResourceExtensions.GetArgumentValuesAsync(and, Hosting.DistributedApplicationOperation.Publish);
        Assert.Contains("-f", winArgs); Assert.Contains("-f", macArgs); Assert.Contains("-f", andArgs);
        Assert.Contains("-p:OpenArguments=-W", macArgs); // MacCatalyst special flag
    }

    [Xunit.Fact]
    public async Task NoPlatformsConfigured_EmitsWarning()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-maccatalyst", "net10.0-android");
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
        var warning = writes.SingleOrDefault(w => w.LogLevel == Microsoft.Extensions.Logging.LogLevel.Warning && (w.Message?.Contains("No .NET MAUI platform resources were configured") ?? false));
        Assert.NotNull(warning);
        Assert.Contains("WithWindows()", warning!.Message);
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
}
