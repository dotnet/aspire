// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Utils;
using Xunit;

namespace Aspire.Hosting.Garnet.Tests;

public class GarnetPublicApiTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void AddGarnetShouldThrowWhenBuilderIsNull(int overrideIndex)
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "garnet";
        int? port = null;
        IResourceBuilder<ParameterResource>? password = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddGarnet(name, port),
            1 => () => builder.AddGarnet(name, port, password),
            _ => throw new InvalidOperationException()
        };

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    public void AddGarnetShouldThrowWhenNameIsNullOrEmpty(int overrideIndex, bool isNull)
    {
        using var builder = TestDistributedApplicationBuilder.Create();
        var name = isNull ? null! : string.Empty;
        int? port = null;
        IResourceBuilder<ParameterResource>? password = null;

        Action action = overrideIndex switch
        {
            0 => () => builder.AddGarnet(name, port),
            1 => () => builder.AddGarnet(name, port, password),
            _ => throw new InvalidOperationException()
        };

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Fact]
    public void WithDataVolumeShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<GarnetResource> builder = null!;

        var action = () => builder.WithDataVolume();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<GarnetResource> builder = null!;
        const string source = "/data";

        var action = () => builder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithDataBindMountShouldThrowWhenSourceIsNullOrEmpty(bool isNull)
    {
        using var testBuilder = TestDistributedApplicationBuilder.Create();
        var builder = testBuilder.AddGarnet("garnet");
        var source = isNull ? null! : string.Empty;

        var action = () => builder.WithDataBindMount(source);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void WithPersistenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<GarnetResource> builder = null!;

        var action = () => builder.WithPersistence();

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    [Obsolete("This method is obsolete and will be removed in a future version. Use the overload without the keysChangedThreshold parameter.")]
    public void ObsoleteWithPersistenceShouldThrowWhenBuilderIsNull()
    {
        IResourceBuilder<GarnetResource> builder = null!;
        TimeSpan? interval = null;
        long keysChangedThreshold = 0;

        var action = () => builder.WithPersistence(interval, keysChangedThreshold);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    //

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CtorGarnetResourceShouldThrowWhenNameIsNullOrEmpty(bool isNull)
    {
        var name = isNull ? null! : string.Empty;

        var action = () => new GarnetResource(name);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
}
