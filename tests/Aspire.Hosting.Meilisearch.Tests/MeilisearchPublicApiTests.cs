// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Meilisearch.Tests;

public class MeilisearchPublicApiTests
{
    [Fact]
    public void AddMeilisearchContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Meilisearch";

        var action = () => builder.AddMeilisearch(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddMeilisearchContainerShouldThrowWhenNameIsNull()
    {
        IDistributedApplicationBuilder builder = new DistributedApplicationBuilder([]);
        string name = null!;

        var action = () => builder.AddMeilisearch(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithDataShouldThrowWhenBuilderIsNull(bool useVolume)
    {
        IResourceBuilder<MeilisearchResource> builder = null!;

        Func<IResourceBuilder<MeilisearchResource>>? action = null;

        if (useVolume)
        {
            action = () => builder.WithDataVolume();
        }
        else
        {
            const string source = "/data";

            action = () => builder.WithDataBindMount(source);
        }

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void WithDataBindMountShouldThrowWhenSourceIsNull()
    {
        var builder = new DistributedApplicationBuilder([]); 
        var resourceBuilder = builder.AddMeilisearch("Meilisearch");

        string source = null!;

        var action = () => resourceBuilder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void CtorMeilisearchResourceShouldThrowWhenNameIsNull()
    {
        var builder = new DistributedApplicationBuilder([]);
        builder.Configuration["Parameters:masterKey"] = "p@ssw0rd1";
        var masterKey = builder.AddParameter("masterKey");
        const string name = null!;

        var action = () => new MeilisearchResource(name, masterKey.Resource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
    [Fact]
    public void CtorMeilisearchResourceShouldThrowWhenMasterKeyIsNull()
    {
        const string name = "Meilisearch";
        ParameterResource masterKey = null!;

        var action = () => new MeilisearchResource(name, masterKey);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(masterKey), exception.ParamName);
    }
}
