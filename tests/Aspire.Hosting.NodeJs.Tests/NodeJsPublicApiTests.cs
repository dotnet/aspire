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

    [Fact]
    public void AddNodeAppShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        string name = null!;
        var scriptPath = ".\\app.js";

        var action = () => builder.AddNodeApp(name: name, scriptPath: scriptPath);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddNodeAppShouldThrowWhenScriptPathIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = "nodeapp";
        string scriptPath = null!;

        var action = () => builder.AddNodeApp(name: name, scriptPath: scriptPath);

        var exception = Assert.Throws<ArgumentNullException>(action);
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

    [Fact]
    public void AddNpmAppShouldThrowWhenWorkingDirectoryIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = "npmapp";
        string workingDirectory = null!;

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(workingDirectory), exception.ParamName);
    }

    [Fact]
    public void AddNpmAppShouldThrowWhenNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        string name = null!;
        var workingDirectory = ".\\app";

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddNpmAppShouldThrowWhenScriptNameIsNull()
    {
        var builder = DistributedApplication.CreateBuilder();
        var name = "npmapp";
        var workingDirectory = ".\\app";
        string scriptName = null!;

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory, scriptName: scriptName);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(scriptName), exception.ParamName);
    }

    [Fact]
    public void CtorNodeAppResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;
        var command = "start";
        var workingDirectory = ".\\app";

        var action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorNodeAppResourceShouldThrowWhenCommandIsNull()
    {
        var name = "nodeapp";
        string command = null!;
        var workingDirectory = ".\\app";

        var action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(command), exception.ParamName);
    }

    [Fact]
    public void CtorNodeAppResourceShouldThrowWhenWorkingDirectoryIsNull()
    {
        var name = "nodeapp";
        var command = "start";
        string workingDirectory = null!;

        var action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(workingDirectory), exception.ParamName);
    }
}
