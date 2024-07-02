// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class TransportOptionsValidatorTests
{
    [Fact]
    public void ValidationFailsWhenHttpUrlSpecifiedWithAllowSecureTransportSetToFalse()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"The 'applicationUrl' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.",
            result.FailureMessage
            );
    }

    [Fact]
    public void InvalidTransportOptionSucceedValidationInPublishMode()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void InvalidTransportOptionSucceedValidationWithDashboardDisabled()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions()
        {
            DisableDashboard = true
        };
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void InvalidTransportOptionSucceedValidationWithDistributedAppOptionsFlag()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions()
        {
            AllowUnsecuredTransport = true
        };
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void ValidationFailsWithInvalidUrl()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var invalidUrl = "...invalid...url...";
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = invalidUrl;

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"The 'applicationUrl' setting of the launch profile has value '{invalidUrl}' which could not be parsed as a URI.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWithMissingUrl()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"AppHost does not have applicationUrl in launch profile, or {KnownConfigNames.AspNetCoreUrls} environment variable set.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWithStringEmptyUrl()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = string.Empty;

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"AppHost does not have applicationUrl in launch profile, or {KnownConfigNames.AspNetCoreUrls} environment variable set.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWhenResourceUrlNotDefined()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";
        config[KnownConfigNames.ResourceServiceEndpointUrl] = string.Empty;
        config[KnownConfigNames.DashboardOtlpGrpcEndpointUrl] = "https://localhost:1236";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"AppHost does not have the {KnownConfigNames.ResourceServiceEndpointUrl} setting defined.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWhenOtlpUrlNotDefined()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";
        config[KnownConfigNames.ResourceServiceEndpointUrl] = "https://localhost:1235";
        config[KnownConfigNames.DashboardOtlpGrpcEndpointUrl] = string.Empty;

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"AppHost does not have the {KnownConfigNames.DashboardOtlpGrpcEndpointUrl} or {KnownConfigNames.DashboardOtlpHttpEndpointUrl} settings defined. At least one OTLP endpoint must be provided.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWhenResourceServiceUrlMalformed()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var invalidUrl = "...invalid...url...";
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";
        config[KnownConfigNames.ResourceServiceEndpointUrl] = invalidUrl;
        config[KnownConfigNames.DashboardOtlpGrpcEndpointUrl] = "https://localhost:1236";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"The {KnownConfigNames.ResourceServiceEndpointUrl} setting with a value of '{invalidUrl}' could not be parsed as a URI.",
            result.FailureMessage
            );
    }

    [Theory]
    [InlineData(KnownConfigNames.DashboardOtlpGrpcEndpointUrl)]
    [InlineData(KnownConfigNames.DashboardOtlpHttpEndpointUrl)]
    public void ValidationFailsWhenOtlpUrlMalformed(string otlpEndpointConfigName)
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var invalidUrl = "...invalid...url...";
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";
        config[KnownConfigNames.ResourceServiceEndpointUrl] = "https://localhost:1235";
        config[otlpEndpointConfigName] = invalidUrl;

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"The {otlpEndpointConfigName} setting with a value of '{invalidUrl}' could not be parsed as a URI.",
            result.FailureMessage
            );
    }

    [Theory]
    [InlineData(KnownConfigNames.DashboardOtlpGrpcEndpointUrl)]
    [InlineData(KnownConfigNames.DashboardOtlpHttpEndpointUrl)]
    public void ValidationFailsWhenDashboardOtlpUrlIsHttp(string otlpEndpointConfigName)
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";
        config[KnownConfigNames.ResourceServiceEndpointUrl] = "https://localhost:1235";
        config[otlpEndpointConfigName] = "http://localhost:1236";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"The '{otlpEndpointConfigName}' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWhenResourceServiceUrlIsHttp()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";
        config[KnownConfigNames.ResourceServiceEndpointUrl] = "http://localhost:1235";
        config[KnownConfigNames.DashboardOtlpGrpcEndpointUrl] = "https://localhost:1236";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"The '{KnownConfigNames.ResourceServiceEndpointUrl}' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationSucceedsWhenHttpUrlSpecifiedWithAllowSecureTransportSetToTrue()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = true;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void ValidationSucceedsWhenHttpsUrlSpecifiedWithAllowSecureTransportSetToTrue()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = true;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void ValidationSucceedsWhenHttpsUrlSpecifiedWithAllowSecureTransportSetToFalse()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";
        config[KnownConfigNames.DashboardOtlpHttpEndpointUrl] = "https://localhost:1235";
        config[KnownConfigNames.ResourceServiceEndpointUrl] = "https://localhost:1236";

        var validator = new TransportOptionsValidator(config, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }
}
