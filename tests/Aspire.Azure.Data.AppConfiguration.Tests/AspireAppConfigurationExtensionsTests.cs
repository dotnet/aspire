// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Azure.Core;
using Azure.Data.AppConfiguration;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Data.AppConfiguration.Tests;

public class AspireAppConfigurationExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AppConfigEndpointCanBeSetInCode(bool useKeyed)
    {
        var endpoint = new Uri(ConformanceTests.Endpoint);
        var mockTransport = new MockTransport(CreateResponse("""{}"""));

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig", "https://unused.azconfig.io/")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureAppConfigurationClient(
                "appConfig",
                settings => {
                    settings.Endpoint = endpoint;
                    settings.Credential = new EmptyTokenCredential();
                },
                clientBuilder => clientBuilder.ConfigureOptions(options => options.Transport = mockTransport));
                
        }
        else
        {
            builder.AddAzureAppConfigurationClient(
                "appConfig",
                settings => {
                    settings.Endpoint = endpoint;
                    settings.Credential = new EmptyTokenCredential();
                },
                clientBuilder => clientBuilder.ConfigureOptions(options => options.Transport = mockTransport));
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ConfigurationClient>("appConfig") :
            host.Services.GetRequiredService<ConfigurationClient>();

        // ConfigurationClient doesn't have a public property to get the endpoint, so we can't verify it directly.
        // Make a request to trigger the transport and record the URI
        client.GetConfigurationSetting("test-key");

        Assert.NotEmpty(mockTransport.Requests);
        var request = mockTransport.Requests[0];
        Assert.StartsWith(endpoint.ToString(), request.Uri.ToString());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var mockTransport = new MockTransport(CreateResponse("""{}"""));

        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "secrets" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Data:AppConfiguration", key, "Endpoint"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig", ConformanceTests.Endpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureAppConfigurationClient(
            "appConfig",
            settings => settings.Credential = new EmptyTokenCredential(),
            clientBuilder => clientBuilder.ConfigureOptions(options => options.Transport = mockTransport));

        }
        else
        {
            builder.AddAzureAppConfigurationClient(
            "appConfig",
            settings => settings.Credential = new EmptyTokenCredential(),
            clientBuilder => clientBuilder.ConfigureOptions(options => options.Transport = mockTransport));
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ConfigurationClient>("appConfig") :
            host.Services.GetRequiredService<ConfigurationClient>();

        // ConfigurationClient doesn't have a public property to get the endpoint, so we can't verify it directly.
        // Make a request to trigger the transport and record the URI
        client.GetConfigurationSetting("test-key");

        Assert.NotEmpty(mockTransport.Requests);
        var request = mockTransport.Requests[0];
        var endpoint = new Uri(ConformanceTests.Endpoint);
        Assert.StartsWith(endpoint.ToString(), request.Uri.ToString());
    }

    [Fact]
    public void AddsAppConfigurationKeyValuesToConfig()
    {
        var mockTransport = new MockTransport(CreateResponse("""
            {
                "items": [
                    {
                        "key": "test-key-1",
                        "value": "test-value-1"
                    },
                    {
                        "key": "test-key-2",
                        "value": "test-value-2"
                    }
                ]
            }
            """));

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig", ConformanceTests.Endpoint)
        ]);

        builder.Configuration.AddAzureAppConfiguration(
            "appConfig",
            settings => settings.Credential = new EmptyTokenCredential(),
            configureOptions: options => options.ConfigureClientOptions(
                clientOptions => clientOptions.Transport = mockTransport));

        Assert.Equal("test-value-1", builder.Configuration["test-key-1"]);
        Assert.Equal("test-value-2", builder.Configuration["test-key-2"]);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var mockTransport = new MockTransport(
            CreateResponse("""{}"""),
            CreateResponse("""{}"""),
            CreateResponse("""{}"""));

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig1", "https://aspiretests1.vault.azconfig.io/"),
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig2", "https://aspiretests2.vault.azconfig.io/"),
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig3", "https://aspiretests3.vault.azconfig.io/")
        ]);

        builder.AddAzureAppConfigurationClient("appConfig1", settings => settings.Credential = new EmptyTokenCredential(), clientBuilder => clientBuilder.ConfigureOptions(options => options.Transport = mockTransport));
        builder.AddKeyedAzureAppConfigurationClient("appConfig2", settings => settings.Credential = new EmptyTokenCredential(), clientBuilder => clientBuilder.ConfigureOptions(options => options.Transport = mockTransport));
        builder.AddKeyedAzureAppConfigurationClient("appConfig3", settings => settings.Credential = new EmptyTokenCredential(), clientBuilder => clientBuilder.ConfigureOptions(options => options.Transport = mockTransport));

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = host.Services.GetRequiredService<ConfigurationClient>();
        var client2 = host.Services.GetRequiredKeyedService<ConfigurationClient>("appConfig2");
        var client3 = host.Services.GetRequiredKeyedService<ConfigurationClient>("appConfig3");

        //Assert.NotSame(client1, client2);
        //Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);

        // ConfigurationClient doesn't have a public property to get the endpoint, so we can't verify it directly.
        // Make a request to trigger the transport and record the URI
        //client1.GetConfigurationSetting("test-key");
        //Assert.StartsWith("https://aspiretests1.vault.azconfig.io/", mockTransport.Requests.Last().Uri.ToString());
        client2.GetConfigurationSetting("test-key");
        Assert.StartsWith("https://aspiretests2.vault.azconfig.io/", mockTransport.Requests.Last().Uri.ToString());
        client3.GetConfigurationSetting("test-key");
        Assert.StartsWith("https://aspiretests3.vault.azconfig.io/", mockTransport.Requests.Last().Uri.ToString());
    }

    private static MockResponse CreateResponse(string content)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        var response = new MockResponse(200)
        {
            ClientRequestId = Guid.NewGuid().ToString(),
            ContentStream = new MemoryStream(buffer),
        };

        return response;
    }

    internal sealed class EmptyTokenCredential : TokenCredential
    {
        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(string.Empty, DateTimeOffset.MaxValue);
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(new AccessToken(string.Empty, DateTimeOffset.MaxValue));
        }
    }
}
