// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Configuration;
using Aspire.Hosting;
using Xunit;

namespace Aspire.Dashboard.Tests;

public sealed class DashboardOptionsTests
{
    private static DashboardOptions GetValidOptions()
    {
        // The minimal set of options required to pass validation.
        return new()
        {
            Frontend =
            {
                AuthMode = FrontendAuthMode.Unsecured,
                EndpointUrls = "http://localhost:5000"
            },
            Otlp =
            {
                AuthMode = OtlpAuthMode.Unsecured,
                GrpcEndpointUrl = "http://localhost:4317"
            },
        };
    }

    [Fact]
    public void ValidOptions_AreValid()
    {
        var result = new ValidateDashboardOptions().Validate(null, GetValidOptions());

        Assert.Null(result.FailureMessage);
        Assert.True(result.Succeeded);
    }

    #region Frontend options

    [Fact]
    public void FrontendOptions_EmptyEndpointUrl()
    {
        var options = GetValidOptions();
        options.Frontend.EndpointUrls = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("One or more frontend endpoint URLs are not configured. Specify a Dashboard:Frontend:EndpointUrls value.", result.FailureMessage);
    }

    [Fact]
    public void FrontendOptions_InvalidUrl()
    {
        var options = GetValidOptions();
        options.Frontend.EndpointUrls = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("Failed to parse frontend endpoint URLs 'invalid'.", result.FailureMessage);
    }

    [Fact]
    public void FrontendOptions_ValidAndInvalidUrl()
    {
        var options = GetValidOptions();
        options.Frontend.EndpointUrls = "http://localhost:5000;invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("Failed to parse frontend endpoint URLs 'http://localhost:5000;invalid'.", result.FailureMessage);
    }

    #endregion

    #region Resource service client options

    [Fact]
    public void ResourceServiceClientOptions_InvalidUrl()
    {
        var options = GetValidOptions();
        options.ResourceServiceClient.Url = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("Failed to parse resource service client endpoint URL 'invalid'.", result.FailureMessage);
    }

    #endregion

    #region OTLP options

    [Fact]
    public void OtlpOptions_NeitherEndpointSet()
    {
        var options = GetValidOptions();
        options.Otlp.GrpcEndpointUrl = null;
        options.Otlp.HttpEndpointUrl = null;

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal(
            $"Neither OTLP/gRPC or OTLP/HTTP endpoint URLs are configured. Specify either a {DashboardConfigNames.DashboardOtlpGrpcUrlName.EnvVarName} or {DashboardConfigNames.DashboardOtlpHttpUrlName.EnvVarName} value.",
            result.FailureMessage);
    }

    [Fact]
    public void OtlpOptions_gRPC_InvalidUrl()
    {
        var options = GetValidOptions();
        options.Otlp.GrpcEndpointUrl = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("Failed to parse OTLP gRPC endpoint URL 'invalid'.", result.FailureMessage);
    }

    [Fact]
    public void OtlpOptions_HTTP_InvalidUrl()
    {
        var options = GetValidOptions();
        options.Otlp.HttpEndpointUrl = "invalid";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("Failed to parse OTLP HTTP endpoint URL 'invalid'.", result.FailureMessage);
    }

    #endregion

    #region OpenIDConnect options

    [Fact]
    public void OpenIdConnectOptions_NoNameClaimType()
    {
        var options = GetValidOptions();
        options.Frontend.AuthMode = FrontendAuthMode.OpenIdConnect;
        options.Frontend.OpenIdConnect.NameClaimType = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("OpenID Connect claim type for name not configured. Specify a Dashboard:Frontend:OpenIdConnect:NameClaimType value.", result.FailureMessage);
    }

    [Fact]
    public void OpenIdConnectOptions_NoUserNameClaimType()
    {
        var options = GetValidOptions();
        options.Frontend.AuthMode = FrontendAuthMode.OpenIdConnect;
        options.Frontend.OpenIdConnect.UsernameClaimType = "";

        var result = new ValidateDashboardOptions().Validate(null, options);

        Assert.False(result.Succeeded);
        Assert.Equal("OpenID Connect claim type for username not configured. Specify a Dashboard:Frontend:OpenIdConnect:UsernameClaimType value.", result.FailureMessage);
    }

    #endregion
}
