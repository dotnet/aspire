// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Keycloak.Authentication.Tests;

public class AspireKeycloakExtensionTests()
{
    internal const string DefaultKeyName = "keycloak";

    [Fact]
    public void AddKeycloakJwtBearer_SetsJwtBearerAuthority()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var realm = "myrealm";

        builder.Services.AddAuthentication()
                        .AddKeycloakJwtBearer(DefaultKeyName, realm, options =>
                        {
                            options.RequireHttpsMetadata = false;
                        });

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsMonitor<JwtBearerOptions>>()
                                   .Get(JwtBearerDefaults.AuthenticationScheme);

        Assert.Equal(options.Authority, $"https+http://{DefaultKeyName}/realms/{realm}");
    }

    [Fact]
    public void AddKeycloakOpenIdConnect_SetsOpenIdConnectAuthority()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var realm = "myrealm";

        builder.Services.AddAuthentication()
                        .AddKeycloakOpenIdConnect(DefaultKeyName, realm, options =>
                        {
                            options.ClientId = "myclient";
                            options.RequireHttpsMetadata = false;
                        });

        using var host = builder.Build();

        var options = host.Services.GetRequiredService<IOptionsMonitor<OpenIdConnectOptions>>()
                                   .Get(OpenIdConnectDefaults.AuthenticationScheme);

        Assert.Equal(options.Authority, $"https+http://{DefaultKeyName}/realms/{realm}");
    }
}
