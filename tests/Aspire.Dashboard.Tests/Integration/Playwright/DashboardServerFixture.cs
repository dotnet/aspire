// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using Aspire.Dashboard.Configuration;
using Aspire.Dashboard.Model;
using Aspire.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Aspire.Dashboard.Tests.Integration.Playwright;

public class DashboardServerFixture : IAsyncLifetime
{
    public DashboardWebApplication DashboardApp { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        const string aspireDashboardAssemblyName = "Aspire.Dashboard";
        var currentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        var currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var aspireAssemblyDirectory = currentAssemblyDirectory.Replace(currentAssemblyName, aspireDashboardAssemblyName);

        var initialData = new Dictionary<string, string?>
        {
            [DashboardConfigNames.DashboardFrontendUrlName.ConfigKey] = "http://127.0.0.1:0",
            [DashboardConfigNames.DashboardOtlpUrlName.ConfigKey] = "http://127.0.0.1:0",
            [DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = nameof(OtlpAuthMode.Unsecured),
            [DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = nameof(FrontendAuthMode.Unsecured),
            ["Authentication:Schemes:OpenIdConnect:RequireHttpsMetadata"] = "false"
        };

        var config = new ConfigurationManager().AddInMemoryCollection(initialData).Build();

        // Add services to the container.
        DashboardApp = new DashboardWebApplication(
            options: new WebApplicationOptions
            {
                EnvironmentName = "Development",
                ContentRootPath = aspireAssemblyDirectory,
                WebRootPath = Path.Combine(aspireAssemblyDirectory, "wwwroot"),
                ApplicationName = aspireDashboardAssemblyName,
            },
            preConfigureBuilder: builder =>
            {
                builder.Configuration.AddConfiguration(config);
            },
            postConfigureBuilder: builder =>
            {
                builder.Services.RemoveAll<IDashboardClient>();
                builder.Services.AddSingleton<IDashboardClient, MockDashboardClient>();
            });

        await DashboardApp.StartAsync();
    }

    public async Task DisposeAsync()
    {
        await DashboardApp.DisposeAsync();
    }
}
