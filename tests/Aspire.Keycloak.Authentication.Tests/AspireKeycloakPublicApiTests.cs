// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Keycloak.Authentication.Tests;

public class AspireKeycloakPublicApiTests
{
    [Fact]
    public void AddKeycloakJwtBearerShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "Keycloak";
        const string realm = "realm";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerShouldThrowWhenServiceNameIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        string serviceName = null!;
        const string realm = "realm";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerShouldThrowWhenRealmIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        string realm = null!;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "Keycloak";
        const string realm = "realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenServiceNameIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        string serviceName = null!;
        const string realm = "realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenRealmIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        string realm = null!;
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenAuthenticationSchemeIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        const string realm = "realm";
        string authenticationScheme = null!;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(authenticationScheme), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "Keycloak";
        const string realm = "realm";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenServiceNameIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        string serviceName = null!;
        const string realm = "realm";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenRealmIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        string realm = null!;
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "Keycloak";
        const string realm = "realm";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectShouldThrowWhenServiceNameIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        string serviceName = null!;
        const string realm = "realm";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectShouldThrowWhenRealmIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        string realm = null!;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "Keycloak";
        const string realm = "realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenServiceNameIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        string serviceName = null!;
        const string realm = "realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenRealmIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        string realm = null!;
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenAuthenticationSchemeIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        const string realm = "realm";
        string authenticationScheme = null!;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(authenticationScheme), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "Keycloak";
        const string realm = "realm";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenServiceNameIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        string serviceName = null!;
        const string realm = "realm";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenRealmIsNull()
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        string realm = null!;
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }
}
