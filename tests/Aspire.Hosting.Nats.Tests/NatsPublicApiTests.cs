// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Nats.Tests;

public class NatsPublicApiTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNatsContainerShouldThrowWhenBuilderIsNull(bool includePort)
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Nats";

        var action = () => includePort ? builder.AddNats(name, 4222) : builder.AddNats(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddNatsContainerShouldThrowWhenNameIsNull(bool includePort)
    {
        var builder = TestDistributedApplicationBuilder.Create();
        string name = null!;

        var action = () => includePort ? builder.AddNats(name, 4222) : builder.AddNats(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithJetStreamShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<NatsServerResource> builder = null!;

        var action = () => builder.WithJetStream();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<NatsServerResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<NatsServerResource> builder = null!;
        const string source = "/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = TestDistributedApplicationBuilder.Create();
        var nats = builder.AddNats("Nats");
        string source = null!;

        var action = () => nats.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void CtorNatsServerResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;

        var action = () => new NatsServerResource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorNatsServerResourceWithParametersShouldThrowWhenNameIsNull()
    {
        string name = null!;
        var builder = TestDistributedApplicationBuilder.Create();
        var user = builder.AddParameter("user");
        var password = builder.AddParameter("password");

        var action = () => new NatsServerResource(name, user.Resource, password.Resource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void CtorNatsServerResourceWithParametersShouldAcceptNullParameters()
    {
        new NatsServerResource("nats", userName: null, password: null);
    }
}
