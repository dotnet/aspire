using System.Diagnostics;
using System.IO.Hashing;
using System.Text;

namespace Aspire.Hosting;

public static class HostingElasticExtensions
{
    public static IResourceBuilder<ElasticsearchResource> RunElasticWithHttpsDevCertificate(this IResourceBuilder<ElasticsearchResource> builder, int port = 9200, int targetPort = 9200)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            builder
                .RunElasticWithHttpsDevCertificate()
                .WithHttpsEndpoint(port: port, targetPort: targetPort)
                .WithEnvironment("QUARKUS_HTTP_HTTP2", "false");
        }

        return builder;
    }

    public static IResourceBuilder<TResource> RunElasticWithHttpsDevCertificate<TResource>(this IResourceBuilder<TResource> builder)
        where TResource : IResourceWithEnvironment
    {
        const string DEV_CERT_DIR = "/usr/share/elasticsearch/config/certificates";

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Export the ASP.NET Core HTTPS development certificate & private key to PEM files, bind mount them into the container
            // and configure it to use them via the specified environment variables.
            var (certPath, certKeyExportPath) = ExportElasticDevCertificate(builder.ApplicationBuilder);
            var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

            if (builder.Resource is ContainerResource containerResource)
            {
                builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                    .WithBindMount(bindSource, DEV_CERT_DIR, isReadOnly: false);
            }

            builder
                .WithEnvironment("xpack.security.http.ssl.enabled", "true")
                .WithEnvironment("xpack.security.http.ssl.certificate", $"{DEV_CERT_DIR}/dev-cert.pem")
                .WithEnvironment("xpack.security.http.ssl.key", $"{DEV_CERT_DIR}/dev-cert.key");
        }

        return builder;
    }

    private static (string, string) ExportElasticDevCertificate(IDistributedApplicationBuilder builder)
    {
        var appNameHashBytes = XxHash64.Hash(Encoding.Unicode.GetBytes(builder.Environment.ApplicationName).AsSpan());
        var appNameHash = BitConverter.ToString(appNameHashBytes).Replace("-", "").ToLowerInvariant();
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire.{appNameHash}");
        var certExportPath = Path.Combine(tempDir, "dev-cert.pem");
        var certKeyExportPath = Path.Combine(tempDir, "dev-cert.key");

        if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            // Certificate already exported, return the path.
            return (certExportPath, certKeyExportPath);
        }
        else if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }

        var exportProcess = Process.Start("dotnet", $"dev-certs https --export-path \"{certExportPath}\" --format Pem --no-password");

        var exited = exportProcess.WaitForExit(TimeSpan.FromSeconds(5));
        if (exited && File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            return (certExportPath, certKeyExportPath);
        }
        else if (exportProcess.HasExited && exportProcess.ExitCode != 0)
        {
            throw new InvalidOperationException($"HTTPS dev certificate export failed with exit code {exportProcess.ExitCode}");
        }
        else if (!exportProcess.HasExited)
        {
            exportProcess.Kill(true);
            throw new InvalidOperationException("HTTPS dev certificate export timed out");
        }

        throw new InvalidOperationException("HTTPS dev certificate export failed for an unknown reason");
    }
}