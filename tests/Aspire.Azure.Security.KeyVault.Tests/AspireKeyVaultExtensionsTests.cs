// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
}
