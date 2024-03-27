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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext);
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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Publish);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidationFailsWithInvalidUrl()
    {
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var invalidUrl = "...invalid...url...";
        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = invalidUrl;

        var validator = new TransportOptionsValidator(config, executionContext);
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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var validator = new TransportOptionsValidator(config, executionContext);
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
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = string.Empty;

        var validator = new TransportOptionsValidator(config, executionContext);
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"AppHost does not have applicationUrl in launch profile, or {KnownConfigNames.AspNetCoreUrls} environment variable set.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationSucceedsWhenHttpUrlSpecifiedWithAllowSecureTransportSetToTrue()
    {
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = true;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "http://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidationSucceedsWhenHttpsUrlSpecifiedWithAllowSecureTransportSetToTrue()
    {
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = true;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded);
    }

    [Fact]
    public void ValidationSucceedsWhenHttpsUrlSpecifiedWithAllowSecureTransportSetToFalse()
    {
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions();
        options.AllowUnsecureTransport = false;

        var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
        config[KnownConfigNames.AspNetCoreUrls] = "https://localhost:1234";

        var validator = new TransportOptionsValidator(config, executionContext);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded);
    }
}
