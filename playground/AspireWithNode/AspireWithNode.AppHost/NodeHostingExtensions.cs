using Aspire.Hosting.JavaScript;
using Microsoft.Extensions.Hosting;

namespace Aspire.Hosting;

internal static class NodeHostingExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.<see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// </summary>
    public static IResourceBuilder<JavaScriptAppResource> RunWithHttpsDevCertificate(this IResourceBuilder<JavaScriptAppResource> builder, string certFileEnv, string certKeyFileEnv)
    {
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
