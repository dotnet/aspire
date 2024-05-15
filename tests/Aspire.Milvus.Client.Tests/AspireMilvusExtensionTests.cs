// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Components.Common.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Milvus.Client;
using Xunit;

namespace Aspire.Milvus.Client.Tests;
public class AspireMilvusExtensionTests : IClassFixture<MilvusContainerFixture>
{
    private readonly MilvusContainerFixture _containerFixture;

    private string ConnectionString => RequiresDockerTheoryAttribute.IsSupported
                                        ? _containerFixture.GetConnectionString()
                                        : "localhost:19530";

    private string NormalizedConnectionString => ConnectionString;

    public AspireMilvusExtensionTests(MilvusContainerFixture containerFixture)
        => _containerFixture = containerFixture;

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(new[]
        {
            new KeyValuePair<string, string?>("ConnectionStrings:milvus", ConnectionString)
        });

        if (useKeyed)
        {
            builder.AddKeyedMilvusClient("milvus");
        }
        else
        {
            builder.AddMilvusClient("milvus");
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<MilvusClient>("milvus") :
            host.Services.GetRequiredService<MilvusClient>();

        Assert.Equal(NormalizedConnectionString, $"http://{dataSource.Address}/");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        void SetConnectionString(MilvusClientSettings settings) => settings.Endpoint = new Uri(ConnectionString);

        if (useKeyed)
        {
            builder.AddKeyedMilvusClient("milvus", SetConnectionString);
        }
        else
        {
            builder.AddMilvusClient("milvus", SetConnectionString);
        }

        using var host = builder.Build();
        var dataSource = useKeyed ?
            host.Services.GetRequiredKeyedService<MilvusClient>("milvus") :
            host.Services.GetRequiredService<MilvusClient>();

        Assert.Equal(NormalizedConnectionString, $"http://{dataSource.Address}/");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "milvus" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Milvus:Client", key, "Endpoint"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:milvus", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedMilvusClient("milvus");
        }
        else
        {
            builder.AddMilvusClient("milvus");
        }

        using var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<MilvusClient>("milvus") :
            host.Services.GetRequiredService<MilvusClient>();

        Assert.Equal(ConnectionString, $"http://{connection.Address}/");
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", connection.Address);
    }
}
