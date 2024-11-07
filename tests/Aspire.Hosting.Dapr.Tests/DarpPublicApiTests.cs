// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
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
    public void AddDaprShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;

        var action = () => builder.AddDapr();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddDaprComponentShouldThrowWhenBuilderIsNull()
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
    public void AddDaprPubSubShouldThrowWhenBuilderIsNull()
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
    public void AddDaprStateStoreShouldThrowWhenBuilderIsNull()
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

    [Fact]
    public void WithDaprSidecarShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<DaprComponentResource> builder = null!;

        var action = () => builder.WithDaprSidecar();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDaprSidecarWithAppIdShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<DaprComponentResource> builder = null!;
        const string appId = "darp";

        var action = () => builder.WithDaprSidecar(appId);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDaprSidecarWithAppIdShouldThrowWhenAppIdIsNullOrEmpty(bool isNull)
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var darp = builder.AddDaprPubSub("darp");
        var appId = isNull ? null! : string.Empty;

        var action = () => darp.WithDaprSidecar(appId);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(appId), exception.ParamName);
    }

    [Fact]
    public void WithDaprSidecarWithConfigureSidecarShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<DaprComponentResource> builder = null!;
        Action<IResourceBuilder<IDaprSidecarResource>> configureSidecar = (_) => { };

        var action = () => builder.WithDaprSidecar(configureSidecar);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDaprSidecarWithConfigureSidecarShouldThrowWhenConfigureSidecarIsNull()
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var darp = builder.AddDaprPubSub("darp");
        Action<IResourceBuilder<IDaprSidecarResource>> configureSidecar = null!;

        var action = () => darp.WithDaprSidecar(configureSidecar);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(configureSidecar), exception.ParamName);
    }

    [Fact]
    public void WithOptionsShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<IDaprSidecarResource> builder = null!;
        DaprSidecarOptions options = null!;

        var action = () => builder.WithOptions(options);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    private sealed class DaprSidecar(string name) : Resource(name), IDaprSidecarResource;

    [Fact]
    public void WithOptionsShouldThrowWhenOptionsIsNull()
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var darpSidecar = builder.AddResource<IDaprSidecarResource>(new DaprSidecar("darpSidecar"));
        DaprSidecarOptions options = null!;

        var action = () => darpSidecar.WithOptions(options);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(options), exception.ParamName);
    }

    [Fact]
    public void WithReferenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<IDaprSidecarResource> builder = null!;

        IDistributedApplicationBuilder resourceBuilder = TestDistributedApplicationBuilder.Create();
        var component = resourceBuilder.AddResource<IDaprComponentResource>(new DaprComponentResource("darp", "state"));

        var action = () => builder.WithReference(component);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithReferenceShouldThrowWhenComponentIsNull()
    {
        IDistributedApplicationBuilder builder = TestDistributedApplicationBuilder.Create();
        var darpSidecar = builder.AddResource<IDaprSidecarResource>(new DaprSidecar("darpSidecar"));
        IResourceBuilder<IDaprComponentResource> component = null!;

        var action = () => darpSidecar.WithReference(component);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(component), exception.ParamName);
    }
}
