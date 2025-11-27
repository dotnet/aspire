// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography.X509Certificates;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Provides extension methods for <see cref="IResourceConfigurationBuilder"/>.
/// </summary>
public static class ResourceConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds a command line arguments configuration gatherer to the builder.
    /// </summary>
    /// <param name="builder">The builder to add the configuration gatherer to.</param>
    /// <returns>The builder with the configuration gatherer added.</returns>
    public static IResourceConfigurationBuilder WithArguments(this IResourceConfigurationBuilder builder)
    {
        return builder.AddConfigurationGatherer(new ResourceArgumentsConfigurationGatherer());
    }

    /// <summary>
    /// Adds an environment variables configuration gatherer to the builder.
    /// </summary>
    /// <param name="builder">The builder to add the configuration gatherer to.</param>
    /// <returns>The builder with the configuration gatherer added.</returns>
    public static IResourceConfigurationBuilder WithEnvironmentVariables(this IResourceConfigurationBuilder builder)
    {
        return builder.AddConfigurationGatherer(new ResourceEnvironmentVariablesConfigurationGatherer());
    }

    /// <summary>
    /// Adds a certificate trust configuration gatherer to the builder.
    /// </summary>
    /// <param name="builder">The builder to add the configuration gatherer to.</param>
    /// <param name="configContextFactory">A factory function to create the configuration context.</param>
    /// <returns>The builder with the configuration gatherer added.</returns>
    public static IResourceConfigurationBuilder WithCertificateTrust(this IResourceConfigurationBuilder builder, Func<CertificateTrustScope, CertificateTrustConfigurationContext> configContextFactory)
    {
        return builder.AddConfigurationGatherer(new ResourceCertificateTrustConfigurationGatherer(configContextFactory));
    }

    /// <summary>
    /// Adds a server authentication certificate configuration gatherer to the builder.
    /// </summary>
    /// <param name="builder">The builder to add the configuration gatherer to.</param>
    /// <param name="configContextFactory">A factory function to create the configuration context.</param>
    /// <returns>The builder with the configuration gatherer added.</returns>
    public static IResourceConfigurationBuilder WithServerAuthenticationCertificate(this IResourceConfigurationBuilder builder, Func<X509Certificate2, ServerAuthenticationCertificateConfigurationContext> configContextFactory)
    {
        return builder.AddConfigurationGatherer(new ResourceServerAuthenticationCertificateConfigurationGatherer(configContextFactory));
    }
}