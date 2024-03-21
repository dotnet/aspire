// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Search.Documents.Indexes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Search.Documents.Tests;

public class AspireAzureSearchExtensionsTests
{
    private const string SearchEndpoint = "https://aspireazuresearchtests.search.windows.net/";
    private const string ConnectionString = $"Endpoint={SearchEndpoint};Key=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:search", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureSearchClient("search");
        }
        else
        {
            builder.AddAzureSearchClient("search");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<SearchIndexClient>("search") :
            host.Services.GetRequiredService<SearchIndexClient>();

        Assert.NotNull(client);
        Assert.Equal(new Uri(SearchEndpoint), client.Endpoint);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var searchEndpoint = new Uri("https://aspireazuresearchtests.search.windows.net/");
        var key = "fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);

        if (useKeyed)
        {
            builder.AddKeyedAzureSearchClient("search", settings => { settings.Endpoint = searchEndpoint; settings.Key = key; });
        }
        else
        {
            builder.AddAzureSearchClient("search", settings => { settings.Endpoint = searchEndpoint; settings.Key = key; });
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<SearchIndexClient>("search") :
            host.Services.GetRequiredService<SearchIndexClient>();

        Assert.NotNull(client);
        Assert.Equal(new Uri(SearchEndpoint), client.Endpoint);
    }
}
