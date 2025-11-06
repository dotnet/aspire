// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Python;
using Aspire.Hosting.Tests.Utils;
using Aspire.Hosting.Utils;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace Aspire.Hosting.Python.Tests;

public class PythonDebuggerTests(ITestOutputHelper outputHelper)
{
    [Fact]
    public void WithDebuggerProperties_ThrowsOnNullBuilder()
    {
        IResourceBuilder<PythonAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.WithDebuggerProperties(props => { }));

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void WithDebuggerProperties_ThrowsOnNullCallback()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py");

        var exception = Assert.Throws<ArgumentNullException>(() =>
            resourceBuilder.WithDebuggerProperties(null!));

        Assert.Equal("configureDebuggerProperties", exception.ParamName);
    }

    [Fact]
    public void WithDebuggerProperties_AddsAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py")
            .WithDebuggerProperties(props => { });

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);
    }

    [Fact]
    public void WithDebuggerProperties_CanBeChainedMultipleTimes()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var callCount = 0;

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py")
            .WithDebuggerProperties(props => callCount++)
            .WithDebuggerProperties(props => callCount++);

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // Should have two annotations
        var annotations = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().ToList();
        Assert.Equal(2, annotations.Count);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresStopAtEntry()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.StopAtEntry = true;
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        // Create a test properties object to verify the configuration
        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.True(testProps.StopAtEntry);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresJustMyCode()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.JustMyCode = true;
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.True(testProps.JustMyCode);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresDjango()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.Django = true;
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.True(testProps.Django);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresGevent()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.Gevent = true;
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.True(testProps.Gevent);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresPythonArgs()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var expectedArgs = new[] { "-X", "dev", "-W", "default" };

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.PythonArgs = expectedArgs;
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.Equal(expectedArgs, testProps.PythonArgs);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresAutoReload()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.AutoReload = new PythonAutoReloadOptions { Enable = true };
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.NotNull(testProps.AutoReload);
        Assert.True(testProps.AutoReload.Enable);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresMultipleProperties()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.StopAtEntry = true;
                props.JustMyCode = false;
                props.Django = true;
                props.Jinja = false;
                props.PythonArgs = ["-X", "dev"];
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.True(testProps.StopAtEntry);
        Assert.False(testProps.JustMyCode);
        Assert.True(testProps.Django);
        Assert.False(testProps.Jinja);
        Assert.Equal(new[] { "-X", "dev" }, testProps.PythonArgs);
    }

    [Fact]
    public void WithDebugging_ThrowsOnNullBuilder()
    {
        IResourceBuilder<PythonAppResource> builder = null!;

        var exception = Assert.Throws<ArgumentNullException>(() =>
            builder.WithDebugging());

        Assert.Equal("builder", exception.ParamName);
    }

    [Fact]
    public void WithDebugging_AddsDebuggableAnnotation()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py")
            .WithDebugging();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault();

        Assert.NotNull(annotation);
    }

    [Fact]
    public void WithDebugging_ThrowsWhenEntrypointAnnotationNotFound()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);

        // Create a resource without going through AddPythonApp (missing annotations)
        var resource = new PythonAppResource("test", "python", "/tmp");
        var resourceBuilder = builder.CreateResourceBuilder(resource);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            resourceBuilder.WithDebugging());

        Assert.Contains("Python entrypoint annotation not found", exception.Message);
    }

    [Fact]
    public void WithDebugging_CanBeCalledOnScriptResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var exception = Record.Exception(() =>
            builder.AddPythonScript("pythonProject", tempDir.Path, "main.py")
                .WithDebugging());

        Assert.Null(exception);
    }

    [Fact]
    public void WithDebugging_CanBeCalledOnModuleResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var exception = Record.Exception(() =>
            builder.AddPythonModule("pythonProject", tempDir.Path, "flask")
                .WithDebugging());

        Assert.Null(exception);
    }

    [Fact]
    public void WithDebugging_CanBeCalledOnExecutableResource()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var exception = Record.Exception(() =>
            builder.AddPythonExecutable("pythonProject", tempDir.Path, "pytest")
                .WithDebugging());

        Assert.Null(exception);
    }

    [Fact]
    public void WithDebugging_CanBeChainedWithOtherMethods()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var exception = Record.Exception(() =>
            builder.AddPythonScript("pythonProject", tempDir.Path, "main.py")
                .WithDebugging()
                .WithArgs("arg1", "arg2")
                .WithEnvironment("TEST_VAR", "value"));

        Assert.Null(exception);
    }

    [Fact]
    public void WithDebuggerProperties_WorksWithScriptEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py")
            .WithDebuggerProperties(props =>
            {
                props.StopAtEntry = true;
            })
            .WithDebugging();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // Verify both annotations exist
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().SingleOrDefault());
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault());
    }

    [Fact]
    public void WithDebuggerProperties_WorksWithModuleEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonModule("pythonProject", tempDir.Path, "flask")
            .WithDebuggerProperties(props =>
            {
                props.Django = true;
            })
            .WithDebugging();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // Verify both annotations exist
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().SingleOrDefault());
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault());
    }

    [Fact]
    public void WithDebuggerProperties_WorksWithExecutableEntrypoint()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonExecutable("pythonProject", tempDir.Path, "pytest")
            .WithDebuggerProperties(props =>
            {
                props.JustMyCode = false;
            })
            .WithDebugging();

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // Verify both annotations exist
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().SingleOrDefault());
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault());
    }

    [Fact]
    public void WithDebuggerProperties_WorksWithUvicornApp()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddUvicornApp("api", tempDir.Path, "main:app")
            .WithDebuggerProperties(props =>
            {
                props.Jinja = true;
                props.JustMyCode = false;
            });

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<UvicornAppResource>());

        // Verify annotation exists (UvicornApp calls WithDebugging by default)
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().SingleOrDefault());
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault());
    }

    [Fact]
    public void WithDebuggerProperties_CanOverrideDefaultJinjaValue()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.Jinja = false; // Override default true value
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working",
            Jinja = true // Default value
        };

        annotation.ConfigureDebuggerProperties(testProps);

        // Should be overridden to false
        Assert.False(testProps.Jinja);
    }

    [Fact]
    public void AddPythonScript_IncludesDebuggingByDefault()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonScript("pythonProject", tempDir.Path, "main.py");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // AddPythonScript calls WithDebugging by default
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public void AddPythonModule_IncludesDebuggingByDefault()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonModule("pythonProject", tempDir.Path, "flask");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // AddPythonModule calls WithDebugging by default
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public void AddPythonExecutable_DoesNotIncludeDebuggingByDefault()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddPythonExecutable("pythonProject", tempDir.Path, "pytest");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<PythonAppResource>());

        // AddPythonExecutable does NOT call WithDebugging by default
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault();
        Assert.Null(annotation);
    }

    [Fact]
    public void AddUvicornApp_IncludesDebuggingByDefault()
    {
        using var builder = TestDistributedApplicationBuilder.Create().WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var resourceBuilder = builder.AddUvicornApp("api", tempDir.Path, "main:app");

        var app = builder.Build();
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();

        var resource = Assert.Single(appModel.Resources.OfType<UvicornAppResource>());

        // AddUvicornApp calls WithDebugging by default
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault();
        Assert.NotNull(annotation);
    }

    [Fact]
    public void WithDebuggerProperties_AllowsNullValuesForOptionalProperties()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.PythonArgs = null;
                props.Django = null;
                props.Gevent = null;
                props.Purpose = null;
                props.AutoReload = null;
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.Null(testProps.PythonArgs);
        Assert.Null(testProps.Django);
        Assert.Null(testProps.Gevent);
        Assert.Null(testProps.Purpose);
        Assert.Null(testProps.AutoReload);
    }

    [Fact]
    public void WithDebuggerProperties_ConfiguresPurpose()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "myapp");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        var pythonApp = builder.AddPythonScript("myapp", appDirectory, "main.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithDebuggerProperties(props =>
            {
                props.Purpose = "debug-test";
            })
            .WithDebugging();

        var app = builder.Build();

        var resource = pythonApp.Resource;
        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();

        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.Equal("debug-test", testProps.Purpose);
    }

    [Fact]
    public void WithDebuggerProperties_SupportsComplexScenario()
    {
        using var builder = TestDistributedApplicationBuilder.Create(DistributedApplicationOperation.Run).WithTestAndResourceLogging(outputHelper);
        using var tempDir = new TempDirectory();

        var runSessionInfo = new RunSessionInfo
        {
            ProtocolsSupported = ["test"],
            SupportedLaunchConfigurations = ["python"]
        };

        builder.Configuration["DEBUG_SESSION_INFO"] = JsonSerializer.Serialize(runSessionInfo);
        builder.Configuration["DEBUG_SESSION_PORT"] = "5678";

        var appDirectory = Path.Combine(tempDir.Path, "django-app");
        Directory.CreateDirectory(appDirectory);
        var virtualEnvironmentPath = Path.Combine(tempDir.Path, ".venv");
        Directory.CreateDirectory(virtualEnvironmentPath);

        // Complex real-world scenario: Django app with custom debugging configuration
        var pythonApp = builder.AddPythonScript("django-app", appDirectory, "manage.py")
            .WithVirtualEnvironment(virtualEnvironmentPath)
            .WithArgs("runserver", "0.0.0.0:8000")
            .WithDebuggerProperties(props =>
            {
                props.Django = true;
                props.Jinja = true;
                props.StopAtEntry = false;
                props.JustMyCode = true;
                props.PythonArgs = ["-Xfrozen_modules=off"];
                props.AutoReload = new PythonAutoReloadOptions { Enable = true };
            })
            .WithDebugging()
            .WithEnvironment("DJANGO_SETTINGS_MODULE", "myapp.settings")
            .WithHttpEndpoint(port: 8000, env: "PORT");

        var app = builder.Build();

        var resource = pythonApp.Resource;

        // Verify all annotations are present
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().SingleOrDefault());
        Assert.NotNull(resource.Annotations.OfType<PythonExecutableDebuggableAnnotation>().SingleOrDefault());
        Assert.NotNull(resource.Annotations.OfType<PythonEntrypointAnnotation>().SingleOrDefault());
        Assert.NotNull(resource.Annotations.OfType<PythonEnvironmentAnnotation>().SingleOrDefault());

        var annotation = resource.Annotations.OfType<PythonExecutableDebuggerPropertiesAnnotation>().Single();
        var testProps = new PythonDebuggerProperties
        {
            Name = "Test",
            InterpreterPath = "/path/to/python",
            WorkingDirectory = "/working"
        };

        annotation.ConfigureDebuggerProperties(testProps);

        Assert.True(testProps.Django);
        Assert.True(testProps.Jinja);
        Assert.False(testProps.StopAtEntry);
        Assert.True(testProps.JustMyCode);
        Assert.Equal(new[] { "-Xfrozen_modules=off" }, testProps.PythonArgs);
        Assert.NotNull(testProps.AutoReload);
        Assert.True(testProps.AutoReload.Enable);
    }
}
