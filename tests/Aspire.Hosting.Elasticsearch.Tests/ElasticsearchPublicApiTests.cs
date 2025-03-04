// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Elasticsearch.Tests;

public class ElasticsearchPublicApiTests
{
    [Fact]
    public void AddElasticsearchContainerShouldThrowWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Elasticsearch";

        var action = () => builder.AddElasticsearch(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddElasticsearchContainerShouldThrowWhenNameIsNull()
    {
        IDistributedApplicationBuilder builder = new DistributedApplicationBuilder([]);
        string name = null!;

        var action = () => builder.AddElasticsearch(name);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithDataShouldThrowWhenBuilderIsNull(bool useVolume)
    {
        IResourceBuilder<ElasticsearchResource> builder = null!;

        Func<IResourceBuilder<ElasticsearchResource>>? action = null;

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
        var resourceBuilder = builder.AddElasticsearch("Elasticsearch");

        string source = null!;

        var action = () => resourceBuilder.WithDataBindMount(source);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(source), exception.ParamName);
    }

    [Fact]
    public void CtorElasticsearchResourceShouldThrowWhenNameIsNull()
    {
        var builder = new DistributedApplicationBuilder([]);
        var password = builder.AddParameter("Password", "p@ssw0rd1");
        const string name = null!;

        var action = () => new ElasticsearchResource(name, password.Resource);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(name), exception.ParamName);
    }
    [Fact]
    public void CtorElasticsearchResourceShouldThrowWhenPasswordIsNull()
    {
        const string name = "Elasticsearch";
        ParameterResource password = null!;

        var action = () => new ElasticsearchResource(name, password);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(password), exception.ParamName);
    }
}
