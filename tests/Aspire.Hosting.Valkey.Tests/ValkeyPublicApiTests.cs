// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Valkey.Tests;

public class ValkeyPublicApiTests
{
    #region ValkeyBuilderExtensions

    [Fact]
    public void AddValkeyContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Valkey";

        var action = () => builder.AddValkey(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddValkeyContainerShouldThrowWhenNameIsNull()
    {
        IDistributedApplicationBuilder builder = new DistributedApplicationBuilder([]);
        string name = null!;

        var action = () => builder.AddValkey(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<ValkeyResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<ValkeyResource> builder = null!;
        const string source = "Valkey";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var distributedApplicationBuilder = new DistributedApplicationBuilder([]);
        const string name = "Valkey";
        var resource = new ValkeyResource(name);
        var builder = distributedApplicationBuilder.AddResource(resource);
        string source = null!;

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithPersistenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<ValkeyResource> builder = null!;

        var action = () => builder.WithPersistence();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    #endregion

    #region ValkeyResource

    [Fact]
    public void CtorValkeyResourceShouldThrowWhenNameIsNull()
    {
        string name = null!;

        var action = () => new ValkeyResource(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    #endregion
}
