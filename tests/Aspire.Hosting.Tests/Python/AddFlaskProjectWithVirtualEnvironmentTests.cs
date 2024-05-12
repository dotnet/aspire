// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;
using Aspire.Hosting.Python;
using Aspire.Hosting.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Hosting.Tests.Python;

public class AddFlaskProjectWithVirtualEnvironmentTests
{
    private readonly static string s_playgroundDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../playground/python/"));
    private readonly static Regex s_flaskExecutablePattern = new Regex("flask(\\.(exe|bat|cmd|sh|ps1))?$", RegexOptions.IgnoreCase);
    private readonly static Regex s_telemetryExecutablePattern = new Regex("opentelemetry-instrument(\\.(exe|bat|cmd|sh|ps1))?$", RegexOptions.IgnoreCase);

    [LocalOnlyFact("python")]
    public void AddFlaskProject_ExecutesFlask()
    {
        var pythonProjectDirectory = Path.Combine(s_playgroundDirectory, "flask_app");
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFlaskProjectWithVirtualEnvironment("python", pythonProjectDirectory, "main.py");

        var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        Assert.Equal(pythonProjectDirectory, pythonProjectResource.WorkingDirectory);
        Assert.Contains(".venv", pythonProjectResource.Command);
        Assert.Matches(s_flaskExecutablePattern, pythonProjectResource.Command);

        var endpoint = Assert.Single(pythonProjectResource.GetEndpoints());

        Assert.Equal("http", endpoint.EndpointName);
        Assert.Equal("FLASK_RUN_PORT", endpoint.EndpointAnnotation.TargetPortEnvironmentVariable);
        Assert.Equal(5000, endpoint.TargetPort);
    }

    [LocalOnlyFact("python")]
    public void AddInstrumentedFlaskProject_ExecutesInstrumentationTool()
    {
        var pythonProjectDirectory = Path.Combine(s_playgroundDirectory, "instrumented_flask_app");
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddFlaskProjectWithVirtualEnvironment("python", pythonProjectDirectory, "main.py");

        var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        Assert.Equal(pythonProjectDirectory, pythonProjectResource.WorkingDirectory);
        Assert.Contains(".venv", pythonProjectResource.Command);
        Assert.Matches(s_telemetryExecutablePattern, pythonProjectResource.Command);

        var endpoint = Assert.Single(pythonProjectResource.GetEndpoints());

        Assert.Equal("http", endpoint.EndpointName);
        Assert.Equal("FLASK_RUN_PORT", endpoint.EndpointAnnotation.TargetPortEnvironmentVariable);
        Assert.Equal(5000, endpoint.TargetPort);
    }
}
