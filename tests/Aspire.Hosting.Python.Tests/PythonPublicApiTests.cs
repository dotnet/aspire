// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Python.Tests;

public class PythonPublicApiTests
{
    #region PythonProjectResource

    [Fact]
    public void CtorPythonProjectResourceShouldThrowsWhenNameIsNull()
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
    public void CtorPythonProjectResourceShouldThrowsWhenExecutablePathIsNull()
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
    public void CtorPythonProjectResourceShouldThrowsWhenProjectDirectoryIsNull()
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
    public void AddPythonProjectShouldThrowsWhenBuilderIsNull()
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
    public void AddPythonProjectShouldThrowsWhenNameIsNull()
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
    public void AddPythonProjectShouldThrowsWhenProjectDirectoryIsNull()
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
    public void AddPythonProjectShouldThrowsWhenScriptPathIsNull()
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
    public void AddPythonProjectShouldThrowsWhenScriptArgsIsNull()
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
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowsWhenBuilderIsNull()
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
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowsWhenNameIsNull()
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
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowsWhenProjectDirectoryIsNull()
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
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowsWhenScriptPathIsNull()
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
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowsWhenVirtualEnvironmentPathIsNull()
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
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowsWhenScriptArgsIsNull()
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
