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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakJwtBearerShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "realm";

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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
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
        const string serviceName = "Keycloak";
        const string realm = "realm";
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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "realm";
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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        const string realm = "realm";
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
        const string serviceName = "Keycloak";
        const string realm = "realm";
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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "realm";
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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        var realm = isNull ? null! : string.Empty;
        Action<JwtBearerOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakJwtBearer(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "realm";

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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
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
        const string serviceName = "Keycloak";
        const string realm = "realm";
        const string authenticationScheme = "Bearer";

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, authenticationScheme);

        var exception = Assert.Throws<ArgumentNullException>(action);
        Assert.Equal(nameof(builder), exception.ParamName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddKeycloakOpenIdConnectWithAuthenticationSchemeShouldThrowWhenServiceNameIsNullOrEmpty(bool isNull)
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "realm";
        const string authenticationScheme = "Bearer";

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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        var realm = isNull ? null! : string.Empty;
        const string authenticationScheme = "Bearer";

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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        const string realm = "realm";
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
        const string serviceName = "Keycloak";
        const string realm = "realm";
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
        var builder = new AuthenticationBuilder(new ServiceCollection());
        var serviceName = isNull ? null! : string.Empty;
        const string realm = "realm";
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
    public void AddKeycloakOpenIdConnectWithConfigureOptionsShouldThrowWhenRealmIsNullOrEmtpy(bool isNull)
    {
        var builder = new AuthenticationBuilder(new ServiceCollection());
        const string serviceName = "Keycloak";
        var realm = isNull ? null! : string.Empty;
        Action<OpenIdConnectOptions>? configureOptions = null;

        var action = () => builder.AddKeycloakOpenIdConnect(serviceName, realm, configureOptions);

        var exception = isNull
            ? Assert.Throws<ArgumentNullException>(action)
            : Assert.Throws<ArgumentException>(action);
        Assert.Equal(nameof(realm), exception.ParamName);
    }
}
