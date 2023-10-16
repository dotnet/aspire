// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.Text;
using Azure.Core;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Security.KeyVault.Tests;

public class AspireKeyVaultExtensionsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void VaultUriCanBeSetInCode(bool useKeyed)
    {
        var vaultUri = new Uri(ConformanceTests.VaultUri);

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:secrets", "https://unused.vault.azure.net/")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureKeyVaultSecrets("secrets", settings => settings.VaultUri = vaultUri);
        }
        else
        {
            builder.AddAzureKeyVaultSecrets("secrets", settings => settings.VaultUri = vaultUri);
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<SecretClient>("secrets") :
            host.Services.GetRequiredService<SecretClient>();

        Assert.Equal(vaultUri, client.VaultUri);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "secrets" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Security:KeyVault", key, "VaultUri"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:secrets", ConformanceTests.VaultUri)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureKeyVaultSecrets("secrets");
        }
        else
        {
            builder.AddAzureKeyVaultSecrets("secrets");
        }

        var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<SecretClient>("secrets") :
            host.Services.GetRequiredService<SecretClient>();

        Assert.Equal(new Uri(ConformanceTests.VaultUri), client.VaultUri);
    }

    [Fact]
    public void AddsKeyVaultSecretsToConfig()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:secrets", ConformanceTests.VaultUri)
        ]);

        builder.Configuration.AddKeyVaultSecrets("secrets", configureClientOptions: o =>
        {
            o.Transport = new MockTransport(
                CreateResponse("""
                    {
                        "value": [
                            {
                                "id": "https://aspiretests.vault.azure.net/secrets/super-secret-1",
                                "attributes": {
                                    "enabled": true,
                                    "created": 1697066435,
                                    "updated": 1697066435,
                                    "recoveryLevel": "Recoverable+Purgeable",
                                    "recoverableDays": 90
                                }
                            },
                            {
                                "id": "https://aspiretests.vault.azure.net/secrets/super-secret-2",
                                "attributes": {
                                    "enabled": true,
                                    "created": 1692910062,
                                    "updated": 1692910062,
                                    "recoveryLevel": "Recoverable+Purgeable",
                                    "recoverableDays": 90
                                }
                            }
                        ],
                        "nextLink": null
                    }
                    """),
                CreateResponse("""
                    {
                        "value": "Secret 1 Value",
                        "id": "https://aspiretests.vault.azure.net/secrets/super-secret-1/1a78b8580ee548c3be0d146aab3817e2",
                        "attributes": {
                            "enabled": true,
                            "created": 1697066435,
                            "updated": 1697066435,
                            "recoveryLevel": "Recoverable+Purgeable",
                            "recoverableDays": 90
                        }
                    }
                    """),
                CreateResponse("""
                    {
                        "value": "Secret 2 Value",
                        "id": "https://aspiretests.vault.azure.net/secrets/super-secret-2/9b8cc4d1ba7941a9b62c3168a17c039f",
                        "attributes": {
                            "enabled": true,
                            "created": 1692910062,
                            "updated": 1692910062,
                            "recoveryLevel": "Recoverable+Purgeable",
                            "recoverableDays": 90
                        }
                    }
                    """));
        });

        Assert.Equal("Secret 1 Value", builder.Configuration["super-secret-1"]);
        Assert.Equal("Secret 2 Value", builder.Configuration["super-secret-2"]);
    }

    private static MockResponse CreateResponse(string content)
    {
        var buffer = Encoding.UTF8.GetBytes(content);
        var response = new MockResponse(200)
        {
            ClientRequestId = Guid.NewGuid().ToString(),
            ContentStream = new MemoryStream(buffer),
        };

        // Add headers matching current response headers from Key Vault.
        response.AddHeader(new HttpHeader("Cache-Control", "no-cache"));
        response.AddHeader(new HttpHeader("Content-Length", buffer.Length.ToString(CultureInfo.InvariantCulture)));
        response.AddHeader(new HttpHeader("Content-Type", "application/json; charset=utf-8"));
        response.AddHeader(new HttpHeader("Date", DateTimeOffset.UtcNow.ToString("r", CultureInfo.InvariantCulture)));
        response.AddHeader(new HttpHeader("Expires", "-1"));
        response.AddHeader(new HttpHeader("Pragma", "no-cache"));
        response.AddHeader(new HttpHeader("Server", "Microsoft-IIS/10.0"));
        response.AddHeader(new HttpHeader("Strict-Transport-Security", "max-age=31536000;includeSubDomains"));
        response.AddHeader(new HttpHeader("X-AspNet-Version", "4.0.30319"));
        response.AddHeader(new HttpHeader("X-Content-Type-Options", "nosniff"));
        response.AddHeader(new HttpHeader("x-ms-keyvault-network-info", "addr=122.117.106.78;act_addr_fam=InterNetwork;"));
        response.AddHeader(new HttpHeader("x-ms-keyvault-region", "westus"));
        response.AddHeader(new HttpHeader("x-ms-keyvault-service-version", "1.1.0.875"));
        response.AddHeader(new HttpHeader("x-ms-request-id", response.ClientRequestId));
        response.AddHeader(new HttpHeader("X-Powered-By", "ASP.NET"));

        return response;
    }
}
