// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using DnsClient;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Dns;

internal sealed partial class DnsSrvServiceEndPointResolverProvider(
    IOptionsMonitor<DnsSrvServiceEndPointResolverOptions> options,
    ILogger<DnsSrvServiceEndPointResolver> logger,
    IDnsQuery dnsClient,
    TimeProvider timeProvider) : IServiceEndPointProviderFactory
{
    private static readonly string s_serviceAccountPath = Path.Combine($"{Path.DirectorySeparatorChar}var", "run", "secrets", "kubernetes.io", "serviceaccount");
    private static readonly string s_serviceAccountNamespacePath = Path.Combine($"{Path.DirectorySeparatorChar}var", "run", "secrets", "kubernetes.io", "serviceaccount", "namespace");
    private static readonly string s_resolveConfPath = Path.Combine($"{Path.DirectorySeparatorChar}etc", "resolv.conf");
    private readonly string? _querySuffix = options.CurrentValue.QuerySuffix ?? GetKubernetesHostDomain();

    /// <inheritdoc/>
    public bool TryCreateProvider(ServiceEndPointQuery query, [NotNullWhen(true)] out IServiceEndPointProvider? resolver)
    {
        // If a default namespace is not specified, then this provider will attempt to infer the namespace from the service name, but only when running inside Kubernetes.
        // Kubernetes DNS spec: https://github.com/kubernetes/dns/blob/master/docs/specification.md
        // SRV records are available for headless services with named ports. 
        // They take the form $"_{portName}._{protocol}.{serviceName}.{namespace}.{suffix}"
        // The suffix (after the service name) can be parsed from /etc/resolv.conf
        // Otherwise, the namespace can be read from /var/run/secrets/kubernetes.io/serviceaccount/namespace and combined with an assumed suffix of "svc.cluster.local".
        // The protocol is assumed to be "tcp".
        // The portName is the name of the port in the service definition. If the serviceName parses as a URI, we use the scheme as the port name, otherwise "default".
        var serviceName = query.OriginalString;
        if (string.IsNullOrWhiteSpace(_querySuffix))
        {
            DnsServiceEndPointResolverBase.Log.NoDnsSuffixFound(logger, serviceName);
            resolver = default;
            return false;
        }

        var portName = query.EndPointName ?? "default";
        var srvQuery = $"_{portName}._tcp.{query.ServiceName}.{_querySuffix}";
        resolver = new DnsSrvServiceEndPointResolver(serviceName, srvQuery, hostName: query.ServiceName, options, logger, dnsClient, timeProvider);
        return true;
    }

    private static string? GetKubernetesHostDomain()
    {
        // Check that we are running in Kubernetes first.
        if (!IsInKubernetesCluster())
        {
            return null;
        }

        if (!OperatingSystem.IsLinux())
        {
            return null;
        }

        var qualifiedNamespace = ReadQualifiedNamespaceFromResolvConf();
        if (!string.IsNullOrWhiteSpace(qualifiedNamespace))
        {
            return qualifiedNamespace;
        }

        var serviceAccountNamespace = ReadNamespaceFromKubernetesServiceAccount();
        if (!string.IsNullOrWhiteSpace(serviceAccountNamespace))
        {
            // The zone is assumed to be "cluster.local"
            return $"{serviceAccountNamespace}.svc.cluster.local";
        }

        return null;
    }

    private static string? ReadNamespaceFromKubernetesServiceAccount()
    {
        // Read the namespace from the Kubernetes pod's service account.
        if (File.Exists(s_serviceAccountNamespacePath))
        {
            return File.ReadAllText(s_serviceAccountNamespacePath).Trim();
        }

        return null;
    }

    private static string? ReadQualifiedNamespaceFromResolvConf()
    {
        if (!File.Exists(s_resolveConfPath))
        {
            return default;
        }

        // See https://manpages.debian.org/bookworm/manpages/resolv.conf.5.en.html#search for the format of /etc/resolv.conf's search option.
        // In our case, we are interested in determining the domain name.
        var lines = File.ReadAllLines(s_resolveConfPath);
        foreach (var line in lines)
        {
            if (!line.StartsWith("search "))
            {
                continue;
            }

            var components = line.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (components.Length > 1)
            {
                return components[1];
            }
        }

        return default;
    }

    private static bool IsInKubernetesCluster()
    {
        // This logic is based on the Kubernetes C# client logic found here:
        // https://github.com/kubernetes-client/csharp/blob/52c3c00d4c55b28bdb491a219f4967823a83df2d/src/KubernetesClient/KubernetesClientConfiguration.InCluster.cs#L21
        var host = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_HOST");
        var port = Environment.GetEnvironmentVariable("KUBERNETES_SERVICE_PORT");
        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port))
        {
            return false;
        }

        var tokenPath = Path.Combine(s_serviceAccountPath, "token");
        if (!File.Exists(tokenPath))
        {
            return false;
        }

        var certPath = Path.Combine(s_serviceAccountPath, "ca.crt");
        return File.Exists(certPath);
    }
}
