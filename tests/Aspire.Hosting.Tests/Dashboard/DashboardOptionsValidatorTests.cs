// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Dashboard;
using Xunit;

namespace Aspire.Hosting.Tests.Dashboard;

public class DashboardOptionsValidatorTests
{
    [Fact]
    public void ValidateFailWhenDashboardUrlIsNullOrEmpty()
    {        
        foreach(string? url in new List<string?> { null, string.Empty })
        {
            var options = new DashboardOptions();
            options.DashboardUrl = url;
            options.OtlpEndpointUrl = "https://localhost";
            
            var validator = new ValidateDashboardOptions();
            var result = validator.Validate(null, options);
            Assert.True(result.Failed);
            Assert.Equal(
                "Failed to configure dashboard resource because ASPNETCORE_URLS environment variable was not set.",
                result.FailureMessage
            );
        }
    }

    [Fact]
    public void ValidateFailWhenOltpEndpointUrlIsNullOrEmpty()
    {        
        foreach(string? url in new List<string?> { null, string.Empty })
        {
            var options = new DashboardOptions();
            options.DashboardUrl = "https://localhost";
            options.OtlpEndpointUrl = url;
            
            var validator = new ValidateDashboardOptions();
            var result = validator.Validate(null, options);
            Assert.True(result.Failed);
            Assert.Equal(
                "Failed to configure dashboard resource because DOTNET_DASHBOARD_OTLP_ENDPOINT_URL environment variable was not set.",
                result.FailureMessage
            );
        }
    }

    [Fact]
    public void ValidateFailWhenDashboardUrlAndOtlpEndpointUrlAreSame()
    {        
        var options = new DashboardOptions();
        options.DashboardUrl = "http://127.0.0.1:9050";
        options.OtlpEndpointUrl = "http://127.0.0.1:9050";
        
        var validator = new ValidateDashboardOptions();
        var result = validator.Validate(null, options);
        Assert.True(result.Failed);
        Assert.Equal(
            $"Failed to configure dashboard resource because ApplicationUrl and DOTNET_DASHBOARD_OTLP_ENDPOINT_URL are both set to {options.DashboardUrl}.",
            result.FailureMessage
        );
    }
}