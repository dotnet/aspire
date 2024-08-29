
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace Aspire.Hosting.Testing.Tests;

public class PublicApiTests
{
    [Fact]
    public async Task CreateAsyncWithOptionsThrowsWhenEntryPointIsNull()
    {
        var ane = await Assert.ThrowsAsync<ArgumentNullException>(() => DistributedApplicationTestingBuilder.CreateAsync(entryPoint: null!));
        Assert.Equal("entryPoint", ane.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsyncWithOptionsThrowsWhenArgsIsNull(bool useGenericOverload)
    {
        Func<Task> action = useGenericOverload
                ? () => DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args: null!)
                : () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint: typeof(Projects.TestingAppHost1_AppHost), args: null!);

        var ane = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal("args", ane.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CreateAsyncWithOptionsThrowsWhenConfigureBuilderIsNull(bool useGenericOverload)
    {
        Func<Task> action = useGenericOverload
                ? () => DistributedApplicationTestingBuilder.CreateAsync<Projects.TestingAppHost1_AppHost>(args: [], configureBuilder: null!)
                : () => DistributedApplicationTestingBuilder.CreateAsync(entryPoint: typeof(Projects.TestingAppHost1_AppHost), args: [], configureBuilder: null!);

        var ane = await Assert.ThrowsAsync<ArgumentNullException>(action);
        Assert.Equal("configureBuilder", ane.ParamName);
    }
}
