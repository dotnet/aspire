// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Python.Tests;

public class PythonPublicApiTests
{
    [Fact]
    public void CtorPythonAppResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        const string executablePath = "/src/python";
        const string projectDirectory = "/data/python";

        var action = () => new PythonAppResource(name, executablePath, projectDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorPythonAppResourceShouldThrowWhenExecutablePathIsNull()
    {
        const string name = "Python";
        string executablePath = null!;
        const string projectDirectory = "/data/python";

        var action = () => new PythonAppResource(name, executablePath, projectDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(executablePath), exception.ParamName);
    }

    [Fact]
    public void CtorPythonAppResourceShouldThrowWhenProjectDirectoryIsNull()
    {
        const string name = "Python";
        const string executablePath = "/src/python";
        string projectDirectory = null!;

        var action = () => new PythonAppResource(name, executablePath, projectDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(projectDirectory), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppShouldThrowWhenNameIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        string name = null!;
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppShouldThrowWhenProjectDirectoryIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        string projectDirectory = null!;
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(projectDirectory), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppShouldThrowWhenScriptPathIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        string scriptPath = null!;
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppShouldThrowWhenScriptArgsIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = null!;

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        var virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenNameIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        string name = null!;
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenProjectDirectoryIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        string projectDirectory = null!;
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(projectDirectory), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenScriptPathIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        string scriptPath = null!;
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenVirtualEnvironmentPathIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string virtualEnvironmentPath = null!;
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(virtualEnvironmentPath), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenScriptArgsIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = null!;

        var action = () => builder.AddPythonApp(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
    }
}
