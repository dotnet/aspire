// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Python.Tests;

public class PythonPublicApiTests
{
    #region PythonProjectResource

    [Fact]
    public void CtorPythonProjectResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        const string executablePath = "/src/python";
        const string projectDirectory = "/data/python";

        var action = () => new PythonProjectResource(name, executablePath, projectDirectory);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Fact]
    public void CtorPythonProjectResourceShouldThrowWhenExecutablePathIsNull()
    {
        const string name = "Python";
        string executablePath = null!;
        const string projectDirectory = "/data/python";

        var action = () => new PythonProjectResource(name, executablePath, projectDirectory);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal("command", exception.ParamName);
        });
    }

    [Fact]
    public void CtorPythonProjectResourceShouldThrowWhenProjectDirectoryIsNull()
    {
        const string name = "Python";
        const string executablePath = "/src/python";
        string projectDirectory = null!;

        var action = () => new PythonProjectResource(name, executablePath, projectDirectory);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal("workingDirectory", exception.ParamName);
        });
    }

    #endregion

    #region PythonProjectResourceBuilderExtensions

    [Fact]
    public void AddPythonProjectShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        string name = null!;
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectShouldThrowWhenProjectDirectoryIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string name = "Python";
        string projectDirectory = null!;
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(projectDirectory), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectShouldThrowWhenScriptPathIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        string scriptPath = null!;
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(scriptPath), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectShouldThrowWhenScriptArgsIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = null!;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(scriptArgs), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        var virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        string name = null!;
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenProjectDirectoryIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string name = "Python";
        string projectDirectory = null!;
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(projectDirectory), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenScriptPathIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        string scriptPath = null!;
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(scriptPath), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenVirtualEnvironmentPathIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string virtualEnvironmentPath = null!;
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(virtualEnvironmentPath), exception.ParamName);
        });
    }

    [Fact]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenScriptArgsIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = null!;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(scriptArgs), exception.ParamName);
        });
    }

    #endregion
}
