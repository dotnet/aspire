// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Maui.Tests;

public class MauiMacCatalystTests
{
    [Xunit.Fact]
    public async System.Threading.Tasks.Task AddsOpenArgumentsFlagWhenNotProvided()
    {
        var csproj = MauiTestHelpers.CreateProject("net10.0-maccatalyst", "net10.0-windows10.0.19041.0");
        var builder = Aspire.Hosting.DistributedApplication.CreateBuilder();
        builder.AddMauiProject("maui", csproj).WithMacCatalyst();
        using var app = builder.Build();

        var model = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Aspire.Hosting.ApplicationModel.DistributedApplicationModel>(app.Services);
        var macRes = Assert.Single(model.Resources.OfType<Aspire.Hosting.ApplicationModel.ProjectResource>(), r => r.Name == "maui-maccatalyst");

        // Retrieve processed argument values using the public extension API (async call). We expect OpenArguments flag added automatically.
        var args = await global::Aspire.Hosting.ApplicationModel.ResourceExtensions.GetArgumentValuesAsync(macRes);
        Assert.Contains("-p:OpenArguments=-W", args);
    }
}
