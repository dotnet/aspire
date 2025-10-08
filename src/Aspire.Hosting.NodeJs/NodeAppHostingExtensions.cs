// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring <see cref="NodeAppResource"/> instances.
/// </summary>
public static class NodeAppHostingExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the Node.js application via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.
    /// </summary>
    /// <param name="builder">The resource builder for the Node.js application.</param>
    /// <param name="certFileEnv">The name of the environment variable that will contain the path to the certificate file.</param>
    /// <param name="certKeyFileEnv">The name of the environment variable that will contain the path to the certificate key file.</param>
    /// <returns>The <see cref="IResourceBuilder{NodeAppResource}"/>.</returns>
    /// <remarks>
    /// <para>
    /// This extension method configures the Node.js application to use the ASP.NET Core HTTPS development certificate
    /// during development. The certificate and key file paths are injected via environment variables, and an HTTPS endpoint
    /// is automatically configured on the resource.
    /// </para>
    /// <para>
    /// The Node.js application should be configured to trust the certificate as a root CA by setting the
    /// NODE_EXTRA_CA_CERTS environment variable to the certificate path.
    /// </para>
    /// </remarks>
    public static IResourceBuilder<NodeAppResource> RunWithHttpsDevCertificate(
        this IResourceBuilder<NodeAppResource> builder,
        string certFileEnv,
        string certKeyFileEnv)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrEmpty(certFileEnv);
        ArgumentException.ThrowIfNullOrEmpty(certKeyFileEnv);

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode && builder.ApplicationBuilder.Environment.IsDevelopment())
        {
            DevCertHostingExtensions.RunWithHttpsDevCertificate(builder, certFileEnv, certKeyFileEnv, (certFilePath, certKeyPath) =>
            {
                builder.WithHttpsEndpoint(env: "HTTPS_PORT");
                var httpsEndpoint = builder.GetEndpoint("https");

                builder.WithEnvironment(context =>
                {
                    // Configure Node to trust the ASP.NET Core HTTPS development certificate as a root CA.
                    if (context.EnvironmentVariables.TryGetValue(certFileEnv, out var certPath))
                    {
                        context.EnvironmentVariables["NODE_EXTRA_CA_CERTS"] = certPath;
                        context.EnvironmentVariables["HTTPS_REDIRECT_PORT"] = ReferenceExpression.Create($"{httpsEndpoint.Property(EndpointProperty.Port)}");
                    }
                });
            });
        }

        return builder;
    }
}
