// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.NodeJs.Tests;

public class NodeJsPublicApiTests
{
    [Fact]
    public void AddNodeAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        var name = "nodeapp";
        var scriptPath = ".\\app.js";

        var action = () => builder.AddNodeApp(name: name, scriptPath: scriptPath);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNodeAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = isNull ? null! : string.Empty;
        var scriptPath = ".\\app.js";

        var action = () => builder.AddNodeApp(name: name, scriptPath: scriptPath);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNodeAppShouldThrowWhenScriptPathIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = "nodeapp";
        var scriptPath = isNull ? null! : string.Empty;

        var action = () => builder.AddNodeApp(name: name, scriptPath: scriptPath);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Fact]
    public void AddNpmAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        var name = "npmapp";
        var workingDirectory = ".\\app";

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNpmAppShouldThrowWhenWorkingDirectoryIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = "npmapp";
        var workingDirectory = isNull ? null! : string.Empty;

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(workingDirectory), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNpmAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = isNull ? null! : string.Empty;
        var workingDirectory = ".\\app";

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNpmAppShouldThrowWhenScriptNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = "npmapp";
        var workingDirectory = ".\\app";
        var scriptName = isNull ? null! : string.Empty;

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory, scriptName: scriptName);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorNodeAppResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        var command = "start";
        var workingDirectory = ".\\app";

        var action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorNodeAppResourceShouldThrowWhenCommandIsNullOrEmpty(bool isNull)
    {
        var name = "nodeapp";
        var command = isNull ? null! : string.Empty;
        var workingDirectory = ".\\app";

        var action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(command), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorNodeAppResourceShouldThrowWhenWorkingDirectoryIsNullOrEmpty(bool isNull)
    {
        var name = "nodeapp";
        var command = "start";
        var workingDirectory = isNull ? null! : string.Empty;

        var action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(workingDirectory), exception.ParamName);
    }
}
