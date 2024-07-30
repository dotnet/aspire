// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Xunit;

namespace Aspire.Hosting.Elasticsearch.Tests;

public class ElasticsearchPublicApiTests
{
    [Fact]
    public void AddElasticsearchContainerShouldThrowsWhenBuilderIsNull()
    {
        IDistributedApplicationBuilder builder = null!;
        const string name = "Elasticsearch";

        var action = () => builder.AddElasticsearch(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void AddElasticsearchContainerShouldThrowsWhenNameIsNull()
    {
        IDistributedApplicationBuilder builder = new DistributedApplicationBuilder([]);
        string name = null!;

        var action = () => builder.AddElasticsearch(name);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void WithDataShouldThrowsWhenBuilderIsNull(bool useVolume)
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

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(builder), exception.ParamName);
        });
    }

    [Fact]
    public void WithDataBindMountShouldThrowsWhenSourceIsNull()
    {
        var builder = new DistributedApplicationBuilder([]); 
        var resourceBuilder = builder.AddElasticsearch("Elasticsearch");

        string source = null!;

        var action = () => resourceBuilder.WithDataBindMount(source);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(source), exception.ParamName);
        });
    }

    [Fact]
    public void CtorElasticsearchResourceShouldThrowsWhenNameIsNull()
    {
        var builder = new DistributedApplicationBuilder([]);
        builder.Configuration["Parameters:Password"] = "p@ssw0rd";
        var password = builder.AddParameter("Password");
        const string name = null!;

        var action = () => new ElasticsearchResource(name, password.Resource);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(name), exception.ParamName);
        });
    }
    [Fact]
    public void CtorElasticsearchResourceShouldThrowsWhenPasswordIsNull()
    {
        var builder = new DistributedApplicationBuilder([]);
        const string name = "Elasticsearch";
        ParameterResource password = null!;

        var action = () => new ElasticsearchResource(name, password);

        Assert.Multiple(() =>
        {
            var exception = Assert.Throws<ArgumentNullException>(action);
            Assert.Equal(nameof(password), exception.ParamName);
        });
    }
}
