// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.NodeJs.Tests;

public class NodeJsPublicApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorNodeAppResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string command = "npm";
        const string workingDirectory = ".\\app";

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
        const string name = "NodeApp";
        var command = isNull ? null! : string.Empty;
        const string workingDirectory = ".\\app";

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
        const string name = "NodeApp";
        const string command = "npm";
        var workingDirectory = isNull ? null! : string.Empty;

        var action = () => new NodeAppResource(name, command, workingDirectory);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(workingDirectory), exception.ParamName);
    }

    [Fact]
    public void AddNodeAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "NodeApp";
        const string scriptPath = ".\\app.js";

        var action = () => builder.AddNodeApp(name, scriptPath);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNodeAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string scriptPath = ".\\app.js";

        var action = () => builder.AddNodeApp(name, scriptPath);

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
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "NodeApp";
        var scriptPath = isNull ? null! : string.Empty;

        var action = () => builder.AddNodeApp(name, scriptPath);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptPath), exception.ParamName);
    }

    [Fact]
    public void AddNpmAppShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "NpmApp";
        const string workingDirectory = ".\\app";

        var action = () => builder.AddNpmApp(name: name, workingDirectory: workingDirectory);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNpmAppShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string workingDirectory = ".\\app";

        var action = () => builder.AddNpmApp(name: name, workingDirectory);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNpmAppShouldThrowWhenWorkingDirectoryIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "NpmApp";
        var workingDirectory = isNull ? null! : string.Empty;

        var action = () => builder.AddNpmApp(name, workingDirectory);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(workingDirectory), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNpmAppShouldThrowWhenScriptNameIsNullOrEmpty(bool isNull)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        const string name = "NpmApp";
        const string workingDirectory = ".\\app";
        var scriptName = isNull ? null! : string.Empty;

        var action = () => builder.AddNpmApp(name, workingDirectory, scriptName);

        var exception = isNull
             ? Assert.Throws<ArgumentNullException>(action)
             : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(scriptName), exception.ParamName);
    }
}
