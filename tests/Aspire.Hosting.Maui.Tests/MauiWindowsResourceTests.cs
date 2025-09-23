// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Aspire.Hosting.Maui.Tests;

public class MauiWindowsResourceTests
{
    [Xunit.Fact]
    public void WithWindows_AddsWindowsResource_WithExplicitStartupAnnotation()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-windows10.0.19041.0", "net10.0-android");
        var builder = Aspire.Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj).WithWindows();
        using var app = builder.Build();

        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Aspire.Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var winRes = Assert.Single(model.Resources.OfType<Aspire.Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-windows");
        Assert.Contains(winRes.Annotations, a => a is Aspire.Hosting.ApplicationModel.ExplicitStartupAnnotation);
    }
}
