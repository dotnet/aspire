// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiPlatformSelectionTests
{
    [Xunit.Fact]
    public void UnknownPlatformIsIgnored()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0");
        var builder = Aspire.Hosting.DistributedApplication.CreateBuilder();
        // Call MacCatalyst even though not targeted
        builder.AddMauiProject("maui", csproj).WithMacCatalyst();
        using var app = builder.Build();

        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Aspire.Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        Assert.DoesNotContain(model.Resources.OfType<Aspire.Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-maccatalyst");
    }

    [Xunit.Fact]
    public async System.Threading.Tasks.Task MultiplePlatforms_AllResourcesPresent()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-maccatalyst", "net10.0-android");
        var builder = Aspire.Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj)
               .WithWindows()
               .WithMacCatalyst()
               .WithAndroid();
        using var app = builder.Build();

        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Aspire.Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var resources = model.Resources.OfType<Aspire.Hosting.ApplicationModel.ProjectResource>().ToList();

        var win = Assert.Single(resources, r => r.Name == "maui-windows");
        var mac = Assert.Single(resources, r => r.Name == "maui-maccatalyst");
        var and = Assert.Single(resources, r => r.Name == "maui-android");

        Assert.All(new[]{win,mac,and}, r => Assert.Contains(r.Annotations, a => a is Aspire.Hosting.ApplicationModel.ExplicitStartupAnnotation));

        // Validate key framework args appear
        var winArgs = await global::Aspire.Hosting.ApplicationModel.ResourceExtensions.GetArgumentValuesAsync(win);
        var macArgs = await global::Aspire.Hosting.ApplicationModel.ResourceExtensions.GetArgumentValuesAsync(mac);
        var andArgs = await global::Aspire.Hosting.ApplicationModel.ResourceExtensions.GetArgumentValuesAsync(and);

        Assert.Contains("-f", winArgs); Assert.Contains("-f", macArgs); Assert.Contains("-f", andArgs);
        Assert.Contains("-p:OpenArguments=-W", macArgs); // MacCatalyst special flag
    }
}
