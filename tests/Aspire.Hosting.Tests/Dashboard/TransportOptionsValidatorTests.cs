// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Microsoft.Extensions.Options;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class TransportOptionsValidatorTests
{
    [Fact]
    public void ValidationFailsWhenHttpUrlSpecifiedWithAllowUnsecureTransportSetToFalse()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "http://localhost:1234"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
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
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "http://localhost:1234"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
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
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "http://localhost:1234"
        });

        var validator = new TransportOptionsValidator(dashboardOptions,executionContext, distributedApplicationOptions);
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
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "http://localhost:1234"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);
        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void ValidationFailsWithInvalidUrl()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };

        var invalidUrl = "...invalid...url...";
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = invalidUrl
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
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
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions());

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
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
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = string.Empty
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
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
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234",
            OtlpGrpcEndpointUrl = "https://localhost:1236",
            ResourceServiceUrl = string.Empty
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
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
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234",
            OtlpGrpcEndpointUrl = string.Empty,
            ResourceServiceUrl = "https://localhost:1235"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
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
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var invalidUrl = "...invalid...url...";
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234",
            OtlpGrpcEndpointUrl = "https://localhost:1236",
            ResourceServiceUrl = invalidUrl
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(
            $"The {KnownConfigNames.ResourceServiceEndpointUrl} setting with a value of '{invalidUrl}' could not be parsed as a URI.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWhenOtlpUrlMalformed()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var invalidUrl = "...invalid...url...";
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234",
            OtlpGrpcEndpointUrl = invalidUrl,
            ResourceServiceUrl = "https://localhost:1235"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(
            $"The {KnownConfigNames.DashboardOtlpGrpcEndpointUrl} setting with a value of '{invalidUrl}' could not be parsed as a URI.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWhenDashboardOtlpUrlIsHttp()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234",
            OtlpGrpcEndpointUrl = "http://localhost:1236",
            ResourceServiceUrl = "https://localhost:1235"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(
            $"The {KnownConfigNames.DashboardOtlpGrpcEndpointUrl} setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationFailsWhenResourceServiceUrlIsHttp()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234",
            OtlpGrpcEndpointUrl = "http://localhost:1236",
            ResourceServiceUrl = "http://localhost:1235"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);

        Assert.True(result.Failed);
        Assert.Equal(
            $"The '{KnownConfigNames.ResourceServiceEndpointUrl}' setting must be an https address unless the '{KnownConfigNames.AllowUnsecuredTransport}' environment variable is set to true. This configuration is commonly set in the launch profile. See https://aka.ms/dotnet/aspire/allowunsecuredtransport for more details.",
            result.FailureMessage
            );
    }

    [Fact]
    public void ValidationSucceedsWhenHttpUrlSpecifiedWithAllowUnsecureTransportSetToTrue()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = true
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "http://localhost:1234"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void ValidationSucceedsWhenHttpsUrlSpecifiedWithAllowUnsecureTransportSetToTrue()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = true
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded, result.FailureMessage);
    }

    [Fact]
    public void ValidationSucceedsWhenHttpsUrlSpecifiedWithAllowUnsecureTransportSetToFalse()
    {
        var distributedApplicationOptions = new DistributedApplicationOptions();
        var executionContext = new DistributedApplicationExecutionContext(DistributedApplicationOperation.Run);
        var options = new TransportOptions
        {
            AllowUnsecureTransport = false
        };
        var dashboardOptions = Options.Create(new DashboardOptions
        {
            DashboardUrl = "https://localhost:1234",
            OtlpHttpEndpointUrl = "https://localhost:1235",
            ResourceServiceUrl = "https://localhost:1236"
        });

        var validator = new TransportOptionsValidator(dashboardOptions, executionContext, distributedApplicationOptions);
        var result = validator.Validate(null, options);

        Assert.True(result.Succeeded, result.FailureMessage);
    }
}
