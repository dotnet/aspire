// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Keycloak.Authentication.Tests;

public class KeycloakAuthenticationPublicApiTests
{
    [Fact]
    public void AddKeycloakJwtBearerShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(authenticationScheme), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "Bearer";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "Bearer";
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(authenticationScheme), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "openId";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(authenticationScheme), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Fact]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenBuilderIsNull()
    {
        AuthenticationBuilder builder = null!;
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "aspire-realm";
        const string authenticationScheme = "openId";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(serviceName), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenRealmIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "openId";
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeAndConfigureOptionsShouldThrowWhenAuthenticationSchemeIsNullOrEmpty(bool isNull)
    {
        var services = new ServiceCollection();
        AuthenticationBuilder builder = new AuthenticationBuilder(services);
        const string serviceName = "keycloak";
        const string realm = "aspire-realm";
        var authenticationScheme = isNull ? null! : string.Empty;
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(
            serviceName,
            realm,
            authenticationScheme,
            configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(authenticationScheme), exception.ParamName);
    }
}
