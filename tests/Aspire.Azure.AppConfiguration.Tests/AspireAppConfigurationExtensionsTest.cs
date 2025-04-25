// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Text;
using Xunit;

namespace Aspire.Azure.AppConfiguration.Tests;

public class AspireAppConfigurationExtensionsTest
{
    [Fact]
    public void AppConfigEndpointCanBeSetInCode()
    {
        var endpoint = new Uri(ConformanceTests.Endpoint);
        var mockTransport = new MockTransport(CreateResponse("""{}"""));

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig", "https://unused.azconfig.io/")
        ]);

        builder.AddAzureAppConfiguration(
            "appConfig",
            settings => {
                settings.Endpoint = endpoint;
                settings.Credential = new EmptyTokenCredential();
            },
            options => options.ConfigureClientOptions(clientOptions => clientOptions.Transport = mockTransport));

        Assert.NotEmpty(mockTransport.Requests);
        var request = mockTransport.Requests[0];
        Assert.StartsWith(endpoint.ToString(), request.Uri.ToString());
    }

    [Fact]
    public void ConnectionNameWinsOverConfiguration()
    {
        var endpoint = new Uri(ConformanceTests.Endpoint);
        var mockTransport = new MockTransport(CreateResponse("""{}"""));
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:AppConfiguration", null, "Endpoint"), "https://unused.azconfig.io/"),
            new KeyValuePair<string, string?>("ConnectionStrings:appConfig", ConformanceTests.Endpoint)
        ]);

        builder.AddAzureAppConfiguration(
            "appConfig",
            settings =>
            {
                settings.Endpoint = endpoint;
                settings.Credential = new EmptyTokenCredential();
            },
            options => options.ConfigureClientOptions(clientOptions => clientOptions.Transport = mockTransport));

        Assert.NotEmpty(mockTransport.Requests);
        var request = mockTransport.Requests[0];
        Assert.StartsWith(endpoint.ToString(), request.Uri.ToString());
    }

    [Fact]
    public void AddsAppConfigurationToApplication()
    {
        var endpoint = new Uri("https://aspiretests.azconfig.io/");
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
        builder.AddAzureAppConfiguration(
            "appConfig",
            settings =>
            {
                settings.Endpoint = endpoint;
                settings.Credential = new EmptyTokenCredential();
            },
            options => options.ConfigureClientOptions(clientOptions => clientOptions.Transport = mockTransport));

        Assert.Equal("test-value-1", builder.Configuration["test-key-1"]);
        Assert.Equal("test-value-2", builder.Configuration["test-key-2"]);
    }

    [Fact]
    public void AddsAppConfigurationToConfiguration()
    {
        var endpoint = new Uri("https://aspiretests.azconfig.io/");
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

        var configurationManager = new ConfigurationManager();
        configurationManager.AddAzureAppConfiguration(
            "appConfig",
            settings =>
            {
                settings.Endpoint = endpoint;
                settings.Credential = new EmptyTokenCredential();
            },
            options => options.ConfigureClientOptions(clientOptions => clientOptions.Transport = mockTransport));

        Assert.Equal("test-value-1", configurationManager["test-key-1"]);
        Assert.Equal("test-value-2", configurationManager["test-key-2"]);
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
