// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.Extensions.NETCore.Setup;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Aspire.AWS.Common;

internal abstract class AWSComponent<TSettings, TClient, TClientConfig>
    where TSettings : class, new()
    where TClient : IAmazonService
    where TClientConfig : ClientConfig, new()
{
    // Abstract methods to be implemented in derived classes
    protected abstract void BindSettingsToConfiguration(TSettings settings, IConfiguration configuration);

    protected abstract void BindClientConfigToConfiguration(TClientConfig clientConfig, IConfiguration configuration);

    protected abstract TClient CreateClient(TSettings settings, TClientConfig clientConfig);

    protected abstract IHealthCheck CreateHealthCheck(TClient client, TSettings settings);

    // Example of a method to add a client to the DI container
    internal void AddClient(
        IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName,
        Action<TSettings>? configureSettings = null)
    {
        // Create and bind settings from configuration
        var settings = new TSettings();
        BindSettingsToConfiguration(settings, configuration.GetSection(configurationSectionName));
        configureSettings?.Invoke(settings);

        // Create and bind client configuration
        var clientConfig = new TClientConfig();
        BindClientConfigToConfiguration(clientConfig, configuration.GetSection(configurationSectionName));

        // Add the AWS service client to the services
        services.AddAWSService<TClient>();
    }

    // Method to add health check (example implementation)
    //internal void AddHealthCheck(IServiceCollection services, string healthCheckName, TClient client, TSettings settings)
    //{
    //    services.AddHealthChecks()
    //        .AddCheck(healthCheckName, () => CreateHealthCheck(client, settings));
    //}

    // Other shared functionalities, like handling AWS credentials, can be added here.
}

internal abstract class AWSComponentSimple<TSettings, TClient>
    where TSettings : class, new()
    where TClient : IAmazonService
{
    // Abstract methods to be implemented in derived classes
    protected abstract void BindSettingsToConfiguration(TSettings settings, IConfiguration configuration);

    protected abstract TClient AddClient(TSettings settings, AWSOptions clientConfig);

    protected abstract IHealthCheck CreateHealthCheck(TClient client, TSettings settings);

    // Example of a method to add a client to the DI container
    internal void AddClient(
        IServiceCollection services,
        IConfiguration configuration,
        string configurationSectionName,
        Action<TSettings>? configureSettings = null)
    {
        // Create and bind settings from configuration
        var settings = new TSettings();
        BindSettingsToConfiguration(settings, configuration.GetSection(configurationSectionName));
        configureSettings?.Invoke(settings);

        // Add the AWS service client to the services
        services.AddAWSService<TClient>();
    }

    // Method to add health check (example implementation)
    //internal void AddHealthCheck(IServiceCollection services, string healthCheckName, TClient client, TSettings settings)
    //{
    //    services.AddHealthChecks()
    //        .AddCheck(healthCheckName, () => CreateHealthCheck(client, settings));
    //}

    // Other shared functionalities, like handling AWS credentials, can be added here.
}

internal sealed class S3ComponentSimple : AWSComponentSimple<AwsS3Settings, AmazonS3Client>
{
    protected override void BindSettingsToConfiguration(AwsS3Settings settings, IConfiguration configuration)
    {
        throw new NotImplementedException();
    }

    protected override AmazonS3Client AddClient(AwsS3Settings settings, AWSOptions clientConfig)
    {
        throw new NotImplementedException();
    }

    protected override IHealthCheck CreateHealthCheck(AmazonS3Client client, AwsS3Settings settings)
    {
        throw new NotImplementedException();
    }
}

public sealed class AwsS3Settings
{
    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the Blob Storage health check is enabled or not.</para>
    /// <para>Enabled by default.</para>
    /// </summary>
    public bool HealthChecks { get; set; } = true;

    /// <summary>
    /// <para>Gets or sets a boolean value that indicates whether the OpenTelemetry tracing is enabled or not.</para>
    /// <para>Disabled by default.</para>
    /// </summary>
    /// <remarks>
    /// ActivitySource support in Azure SDK is experimental, the shape of Activities may change in the future without notice.
    /// It can be enabled by setting "Azure.Experimental.EnableActivitySource" <see cref="AppContext"/> switch to true.
    /// Or by setting "AZURE_EXPERIMENTAL_ENABLE_ACTIVITY_SOURCE" environment variable to "true".
    /// </remarks>
    public bool Tracing { get; set; }
}
