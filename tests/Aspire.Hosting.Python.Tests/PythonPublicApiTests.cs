// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;

namespace Aspire.Hosting.Python.Tests;

public class PythonPublicApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorPythonAppResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string executablePath = "/src/python";
        const string appDirectory = "/data/python";

        var action = () => new PythonAppResource(name, executablePath, appDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorPythonAppResourceShouldThrowWhenExecutablePathIsNullOrEmpty(bool isNull)
    {
        const string name = "Python";
        var executablePath = isNull ? null! : string.Empty;
        const string appDirectory = "/data/python";

        var action = () => new PythonAppResource(name, executablePath, appDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal("command", exception.ParamName);
    }

    [Fact]
    public void CtorPythonAppResourceShouldThrowWhenAppDirectoryIsNull()
    {
        const string name = "Python";
        const string executablePath = "/src/python";

        var action = () => new PythonAppResource(name, executablePath, appDirectory: null!);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("workingDirectory", exception.ParamName);
    }

    [Fact]
    public void AddPythonAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Python";
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppShouldThrowWhenAppDirectoryIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        var appDirectory = isNull ? null! : string.Empty;
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(appDirectory), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppShouldThrowWhenScriptPathIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string appDirectory = "/src/python";
        var scriptPath = isNull ? null! : string.Empty;
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Fact]
    public void AddPythonAppShouldThrowWhenScriptArgsIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = null!;

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppShouldThrowWhenScriptArgsContainsIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
        Assert.Equal(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'scriptArgs')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'scriptArgs')",
            exception.Message);
    }

    [Fact]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Python";
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        var virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenAppDirectoryIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        var appDirectory = isNull ? null! : string.Empty;
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(appDirectory), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenScriptPathIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string appDirectory = "/src/python";
        var scriptPath = isNull ? null! : string.Empty;
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenVirtualEnvironmentPathIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        var virtualEnvironmentPath = isNull ? null! : string.Empty;
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(virtualEnvironmentPath), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddPythonAppWithVirtualEnvironmentPathShouldThrowWhenScriptArgsIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string appDirectory = "/src/python";
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => builder.AddPythonApp(
            name,
            appDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
        Assert.Equal(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'scriptArgs')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'scriptArgs')",
            exception.Message);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("PythonProjectResource is deprecated. Please use PythonAppResource instead.")]
    public void CtorPythonProjectResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string executablePath = "/src/python";
        const string appDirectory = "/data/python";

        var action = () => new PythonProjectResource(name, executablePath, appDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("PythonProjectResource is deprecated. Please use PythonAppResource instead.")]
    public void CtorPythonProjectResourceShouldThrowWhenExecutablePathIsNullOrEmpty(bool isNull)
    {
        const string name = "Python";
        var executablePath = isNull ? null! : string.Empty;
        const string appDirectory = "/data/python";

        var action = () => new PythonProjectResource(name, executablePath, appDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal("command", exception.ParamName);
    }

    [Fact]
    [Obsolete("PythonProjectResource is deprecated. Please use PythonAppResource instead.")]
    public void CtorPythonProjectResourceShouldThrowWhenAppDirectoryIsNull()
    {
        const string name = "Python";
        const string executablePath = "/src/python";

        var action = () => new PythonProjectResource(name, executablePath, projectDirectory: null!);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal("workingDirectory", exception.ParamName);
    }

    [Fact]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
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

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectShouldThrowWhenAppDirectoryIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        var projectDirectory = isNull ? null! : string.Empty;
        const string scriptPath = "scripts";
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(projectDirectory), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectThrowWhenScriptPathIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        var scriptPath = isNull ? null! : string.Empty;
        string[] scriptArgs = ["--traces"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Fact]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectShouldThrowWhenScriptArgsIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = null!;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectShouldThrowWhenScriptArgsContainsIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        string[] scriptArgs = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
        Assert.Equal(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'scriptArgs')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'scriptArgs')",
            exception.Message);
    }

    [Fact]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
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

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
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

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenAppDirectoryIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        var projectDirectory = isNull ? null! : string.Empty;
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(projectDirectory), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectpWithVirtualEnvironmentPathShouldThrowWhenScriptPathIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        var scriptPath = isNull ? null! : string.Empty;
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenVirtualEnvironmentPathIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        var virtualEnvironmentPath = isNull ? null! : string.Empty;
        string[] scriptArgs = ["--traces"]; ;

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(virtualEnvironmentPath), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Obsolete("AddPythonProject is deprecated. Please use AddPythonApp instead.")]
    public void AddPythonProjectWithVirtualEnvironmentPathShouldThrowWhenScriptArgsIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "Python";
        const string projectDirectory = "/src/python";
        const string scriptPath = "scripts";
        const string virtualEnvironmentPath = ".venv";
        string[] scriptArgs = ["arg", isNull ? null! : string.Empty, "arg2"];

        var action = () => builder.AddPythonProject(
            name,
            projectDirectory,
            scriptPath,
            virtualEnvironmentPath,
            scriptArgs);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptArgs), exception.ParamName);
        Assert.Equal(isNull
            ? "Array params contains null item: [arg, , arg2] (Parameter 'scriptArgs')"
            : "Array params contains empty item: [arg, , arg2] (Parameter 'scriptArgs')",
            exception.Message);
    }
}
