// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using System.Text.RegularExpressions;

namespace Aspire.Hosting.Tests.Python;

public class AddPythonProjectTests
{
    private static readonly Regex s_pythonExecutablePattern  = new Regex("python(\\.(exe|bat|cmd|sh|ps1))?$", RegexOptions.IgnoreCase);
    private static readonly Regex s_telemetryExecutablePattern = new Regex("opentelemetry-instrument(\\.(exe|bat|cmd|sh|ps1))?$", RegexOptions.IgnoreCase);
    private static readonly string s_playgroundDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../playground/python/"));

    [LocalOnlyFact("python")]
    public void AddPythonProjectWithVirtualEnvironment_ExecutesPython()
    {
        var pythonProjectDirectory = Path.Combine(s_playgroundDirectory, "script_only");
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddPythonProjectWithVirtualEnvironment("python", pythonProjectDirectory, "main.py");

        var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        Assert.Equal(pythonProjectDirectory, pythonProjectResource.WorkingDirectory);
        Assert.Contains(".venv", pythonProjectResource.Command);
        Assert.Matches(s_pythonExecutablePattern, pythonProjectResource.Command);
    }

    [LocalOnlyFact("python")]
    public void AddInstrumentedPythonProject_ExecutesInstrumentationTool()
    {
        var pythonProjectDirectory = Path.Combine(s_playgroundDirectory, "instrumented_script");
        var appBuilder = DistributedApplication.CreateBuilder();

        appBuilder.AddPythonProjectWithVirtualEnvironment("python", pythonProjectDirectory, "main.py");

        var app = appBuilder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var executableResources = appModel.GetExecutableResources();

        var pythonProjectResource = Assert.Single(executableResources);

        Assert.Equal(pythonProjectDirectory, pythonProjectResource.WorkingDirectory);
        Assert.Contains(".venv", pythonProjectResource.Command);
        Assert.Matches(s_telemetryExecutablePattern, pythonProjectResource.Command);
    }
}
