// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Valkey.Tests;

public class ValkeyPublicApiTests
{
    #region ValkeyBuilderExtensions

    [Fact]
    public void AddValkeyContainerShouldThrowsWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Valkey";

        var action = () => builder.AddValkey(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void AddValkeyContainerShouldThrowsWhenNameIsNull()
    {
        IDistributedApplicationBuilder builder = new DistributedApplicationBuilder([]);
        string name = null!;

        var action = () => builder.AddValkey(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataVolumeShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<ValkeyResource> builder = null!;

        var action = () => builder.WithDataVolume();

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataBindMountShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<ValkeyResource> builder = null!;
        const string source = "Valkey";

        var action = () => builder.WithDataBindMount(source);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataBindMountShouldThrowsWhenSourceIsNull()
    {
        var distributedApplicationBuilder = new DistributedApplicationBuilder([]);
        const string name = "Valkey";
        var resource = new ValkeyResource(name);
        var builder = distributedApplicationBuilder.AddResource(resource);
        string source = null!;

        var action = () => builder.WithDataBindMount(source);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(source), exception.ParamName);
        });
    }

    [Fact]
    public void WithPersistenceShouldThrowsWhenBuilderIsNull()
    {
        IResourceBuilder<ValkeyResource> builder = null!;

        var action = () => builder.WithPersistence();

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    #endregion

    #region ValkeyResource

    [Fact]
    public void CtorValkeyResourceShouldThrowsWhenNameIsNull()
    {
        string name = null!;

        var action = () => new ValkeyResource(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    #endregion
}
