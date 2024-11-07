// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.RabbitMQ.Tests;

public class RabbitMQPublicApiTests
{
    [Fact]
    public void AddRabbitMQContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "rabbitMQ";

        var action = () => builder.AddRabbitMQ(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddRabbitMQContainerShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var builder = DistributedApplication.CreateBuilder([]);
        var name = isNull ? null! : string.Empty;

        var action = () => builder.AddRabbitMQ(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;
        const string source = "/rabbitMQ/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        var builderResource = TestDistributedApplicationBuilder.Create();
        var rabbitMQ = builderResource.AddRabbitMQ("rabbitMQ");
        var source = isNull ? null! : string.Empty;

        var action = () => rabbitMQ.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithManagementPluginShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;

        var action = () => builder.WithManagementPlugin();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithManagementPluginAndPortShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<RabbitMQServerResource> builder = null!;

        var action = () => builder.WithManagementPlugin(15672);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorRabbitMQServerResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var distributedApplicationBuilder = DistributedApplication.CreateBuilder([]);
        var name = isNull ? null! : string.Empty;
        var password = ParameterResourceBuilderExtensions.CreateDefaultPasswordParameter(distributedApplicationBuilder, "password", special: false);

        var action = () => new RabbitMQServerResource(name: name, userName: null, password: password);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorRabbitMQServerResourceShouldThrowWhenPasswordIsNull()
    {
        string name = "rabbitMQ";
        ParameterResource password = null!;

        var action = () => new RabbitMQServerResource(name: name, userName: null, password: password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(password), exception.ParamName);
    }
}
