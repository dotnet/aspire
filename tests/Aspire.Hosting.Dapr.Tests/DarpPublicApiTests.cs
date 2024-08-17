// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Dapr.Tests;

public class DarpPublicApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorDaprComponentResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;
        const string type = "state";

        var action = () => new DaprComponentResource(name, type);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorDaprComponentResourceShouldThrowWhenTypeIsNullOrEmpty(bool isNull)
    {
        const string name = "darp";
        var type = isNull ? null! : string.Empty;

        var action = () => new DaprComponentResource(name, type);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(type), exception.ParamName);
    }

    [Fact]
    public void AddDaprShouldThrowWhenbuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddDapr();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddDaprComponentShouldThrowWhenbuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "darp";
        const string type = "state";

        var action = () => builder.AddDaprComponent(name, type);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDaprComponentShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        const string type = "state";

        var action = () => builder.AddDaprComponent(name, type);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDaprComponentShouldThrowWhenTypeIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        const string name = "darp";
        var type = isNull ? null! : string.Empty;

        var action = () => builder.AddDaprComponent(name, type);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(type), exception.ParamName);
    }

    [Fact]
    public void AddDaprPubSubShouldThrowWhenbuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "darp";

        var action = () => builder.AddDaprPubSub(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDaprPubSubShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddDaprPubSub(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void AddDaprStateStoreShouldThrowWhenbuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "darp";

        var action = () => builder.AddDaprStateStore(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddDaprStateStoreShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddDaprStateStore(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
