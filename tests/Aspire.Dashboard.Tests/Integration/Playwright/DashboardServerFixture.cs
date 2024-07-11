﻿// Licensed to the .NET Foundation under one or more agreements.
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

    public Task InitializeAsync()
    {
        const string aspireDashboardAssemblyName = "Aspire.Dashboard";
        var currentAssemblyName = Assembly.GetExecutingAssembly().GetName().Name!;
        var currentAssemblyDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        var aspireAssemblyDirectory = currentAssemblyDirectory.Replace(currentAssemblyName, aspireDashboardAssemblyName);

        var initialData = new Dictionary<string, string?>
        {
            [DashboardConfigNames.DashboardFrontendUrlName.ConfigKey] = "http://127.0.0.1:0",
            [DashboardConfigNames.DashboardOtlpHttpUrlName.ConfigKey] = "http://127.0.0.1:0",
            [DashboardConfigNames.DashboardOtlpAuthModeName.ConfigKey] = nameof(OtlpAuthMode.Unsecured),
            [DashboardConfigNames.DashboardFrontendAuthModeName.ConfigKey] = nameof(FrontendAuthMode.Unsecured)
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

        return DashboardApp.StartAsync();
    }

    public Task DisposeAsync()
    {
        return DashboardApp.DisposeAsync().AsTask();
    }
}
