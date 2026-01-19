// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Aspire.Cli.Dcp;

/// <summary>
/// Client for communicating with DCP's Kubernetes-style API using HTTP.
/// </summary>
internal sealed class DcpClient : IDcpClient, IDisposable
{
    private const string ApiGroup = "usvc-dev.developer.microsoft.com";
    private const string ApiVersion = "v1";
    private const string ExecutablesResource = "executables";
    private const string ServicesResource = "services";

    private readonly ILogger<DcpClient> _logger;
    private HttpClient _httpClient;
    private string? _baseUrl;

    public DcpClient(ILogger<DcpClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task ConnectAsync(string kubeconfigPath, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Reading kubeconfig from {Path}", kubeconfigPath);

        var kubeconfigContent = await File.ReadAllTextAsync(kubeconfigPath, cancellationToken);
        var serverUrl = ParseKubeconfigServerUrl(kubeconfigContent);
        var token = ParseKubeconfigToken(kubeconfigContent);

        if (string.IsNullOrEmpty(serverUrl))
        {
            throw new InvalidOperationException("Kubeconfig does not contain a valid server URL.");
        }

        _baseUrl = serverUrl;

        // Configure HttpClient to skip certificate validation for localhost (DCP uses self-signed certs)
        // Handle IPv4 localhost, IPv6 localhost [::1], and named localhost
        HttpClientHandler? handler = null;
        if (_baseUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase) ||
            _baseUrl.Contains("127.0.0.1", StringComparison.Ordinal) ||
            _baseUrl.Contains("[::1]", StringComparison.Ordinal))
        {
            handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        }

        _httpClient.Dispose();
        _httpClient = handler != null ? new HttpClient(handler) : new HttpClient();

        // Add authentication token if present
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        _logger.LogDebug("Connected to DCP at {BaseUrl}", _baseUrl);
    }

    public async Task<DcpExecutableResource> CreateExecutableAsync(DcpExecutableSpec spec, CancellationToken cancellationToken)
    {
        EnsureConnected();

        var envVars = spec.Env?.Select(kv => new DcpEnvVar { Name = kv.Key, Value = kv.Value }).ToList();

        var metadata = new DcpObjectMetadata { Name = spec.Name, NamespaceProperty = "" };
        if (spec.Annotations != null && spec.Annotations.Count > 0)
        {
            metadata.Annotations = new Dictionary<string, string>(spec.Annotations);
        }

        var executable = new DcpExecutableRequest
        {
            ApiVersion = $"{ApiGroup}/{ApiVersion}",
            Kind = "Executable",
            Metadata = metadata,
            Spec = new DcpExecutableSpecRequest
            {
                ExecutablePath = spec.ExecutablePath,
                WorkingDirectory = spec.WorkingDirectory,
                Args = spec.Args?.ToList(),
                Env = envVars
            }
        };

        var url = $"{_baseUrl}/apis/{ApiGroup}/{ApiVersion}/{ExecutablesResource}";
        _logger.LogDebug("Creating executable {Name} at {Url}", spec.Name, url);

        var json = JsonSerializer.Serialize(executable, DcpJsonContext.Default.DcpExecutableRequest);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(responseJson, DcpJsonContext.Default.DcpExecutableResponse);
        return MapToResource(result!);
    }

    public async Task<DcpExecutableResource?> GetExecutableAsync(string name, CancellationToken cancellationToken)
    {
        EnsureConnected();

        var url = $"{_baseUrl}/apis/{ApiGroup}/{ApiVersion}/{ExecutablesResource}/{name}";
        _logger.LogDebug("Getting executable {Name} from {Url}", name, url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(responseJson, DcpJsonContext.Default.DcpExecutableResponse);
        return MapToResource(result!);
    }

    public async Task DeleteExecutableAsync(string name, CancellationToken cancellationToken)
    {
        EnsureConnected();

        var url = $"{_baseUrl}/apis/{ApiGroup}/{ApiVersion}/{ExecutablesResource}/{name}";
        _logger.LogDebug("Deleting executable {Name} at {Url}", name, url);

        var response = await _httpClient.DeleteAsync(url, cancellationToken);
        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            response.EnsureSuccessStatusCode();
        }
    }

    public async IAsyncEnumerable<DcpExecutableResource> WatchExecutableAsync(
        string name,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureConnected();

        // Poll for changes (simplified watch implementation)
        while (!cancellationToken.IsCancellationRequested)
        {
            var resource = await GetExecutableAsync(name, cancellationToken);
            if (resource != null)
            {
                yield return resource;

                // If the resource has reached a terminal state, stop watching
                if (resource.State is "Running" or "Terminated" or "FailedToStart" or "Finished")
                {
                    yield break;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    public async Task<DcpServiceResource> CreateServiceAsync(DcpServiceSpec spec, CancellationToken cancellationToken)
    {
        EnsureConnected();

        var service = new DcpServiceRequest
        {
            ApiVersion = $"{ApiGroup}/{ApiVersion}",
            Kind = "Service",
            Metadata = new DcpObjectMetadata { Name = spec.Name, NamespaceProperty = "" },
            Spec = new DcpServiceSpecRequest
            {
                Port = spec.Port,
                Address = spec.Address,
                Protocol = spec.Protocol,
                AddressAllocationMode = spec.AddressAllocationMode
            }
        };

        var url = $"{_baseUrl}/apis/{ApiGroup}/{ApiVersion}/{ServicesResource}";
        _logger.LogDebug("Creating service {Name} at {Url}", spec.Name, url);

        var json = JsonSerializer.Serialize(service, DcpJsonContext.Default.DcpServiceRequest);
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(responseJson, DcpJsonContext.Default.DcpServiceResponse);
        return MapToServiceResource(result!);
    }

    public async Task<DcpServiceResource?> GetServiceAsync(string name, CancellationToken cancellationToken)
    {
        EnsureConnected();

        var url = $"{_baseUrl}/apis/{ApiGroup}/{ApiVersion}/{ServicesResource}/{name}";
        _logger.LogDebug("Getting service {Name} from {Url}", name, url);

        var response = await _httpClient.GetAsync(url, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var result = JsonSerializer.Deserialize(responseJson, DcpJsonContext.Default.DcpServiceResponse);
        return MapToServiceResource(result!);
    }

    public async IAsyncEnumerable<DcpServiceResource> WatchServiceAsync(
        string name,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        EnsureConnected();

        // Poll for changes until service is Ready
        while (!cancellationToken.IsCancellationRequested)
        {
            var resource = await GetServiceAsync(name, cancellationToken);
            if (resource != null)
            {
                yield return resource;

                // If the service is ready, stop watching
                if (resource.State == "Ready")
                {
                    yield break;
                }
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        }
    }

    public async Task<Stream> GetLogStreamAsync(string executableName, string streamType, bool follow, CancellationToken cancellationToken)
    {
        EnsureConnected();

        var followParam = follow.ToString().ToLowerInvariant();
        var url = $"{_baseUrl}/apis/{ApiGroup}/{ApiVersion}/{ExecutablesResource}/{executableName}/log?follow={followParam}&source={streamType}";

        _logger.LogDebug("Getting log stream for executable {Name} from {Url}", executableName, url);

        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    private void EnsureConnected()
    {
        if (string.IsNullOrEmpty(_baseUrl))
        {
            throw new InvalidOperationException("DcpClient is not connected. Call ConnectAsync first.");
        }
    }

    private static DcpExecutableResource MapToResource(DcpExecutableResponse response)
    {
        return new DcpExecutableResource(
            Name: response.Metadata?.Name ?? "",
            State: response.Status?.State,
            Pid: response.Status?.Pid,
            StdOutFile: response.Status?.StdOutFile,
            StdErrFile: response.Status?.StdErrFile);
    }

    private static DcpServiceResource MapToServiceResource(DcpServiceResponse response)
    {
        return new DcpServiceResource(
            Name: response.Metadata?.Name ?? "",
            State: response.Status?.State,
            EffectiveAddress: response.Status?.EffectiveAddress,
            EffectivePort: response.Status?.EffectivePort);
    }

    /// <summary>
    /// Simple kubeconfig parser that extracts the server URL.
    /// Parses YAML without a full YAML library for AOT compatibility.
    /// </summary>
    private static string? ParseKubeconfigServerUrl(string content)
    {
        // Look for server: URL pattern in the clusters section
        // Kubeconfig format:
        // clusters:
        // - cluster:
        //     server: https://...
        var lines = content.Split('\n');
        var inClusters = false;
        var inClusterItem = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("clusters:", StringComparison.Ordinal))
            {
                inClusters = true;
                continue;
            }

            // Handle "- cluster:" which is a list item under clusters
            if (inClusters && trimmed.StartsWith("- cluster:", StringComparison.Ordinal))
            {
                inClusterItem = true;
                continue;
            }

            // Look for server: within the cluster item
            if (inClusterItem && trimmed.StartsWith("server:", StringComparison.Ordinal))
            {
                var serverUrl = trimmed.Substring("server:".Length).Trim();
                if (!string.IsNullOrEmpty(serverUrl))
                {
                    return serverUrl;
                }
            }

            // Reset if we hit a new top-level section (non-indented, non-list-item line)
            if (!string.IsNullOrEmpty(trimmed) && !char.IsWhiteSpace(line[0]) &&
                !trimmed.StartsWith("-", StringComparison.Ordinal) &&
                !trimmed.StartsWith("clusters:", StringComparison.Ordinal))
            {
                if (inClusters)
                {
                    break; // Exit clusters section
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Simple kubeconfig parser that extracts the user token.
    /// </summary>
    private static string? ParseKubeconfigToken(string content)
    {
        // Look for token: pattern in the users section
        // users:
        // - name: apiserver_user
        //   user:
        //     token: xxxxx
        var lines = content.Split('\n');
        var inUsers = false;
        var inUser = false;

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("users:", StringComparison.Ordinal))
            {
                inUsers = true;
                continue;
            }

            // Handle "user:" section under a user item
            if (inUsers && trimmed.StartsWith("user:", StringComparison.Ordinal))
            {
                inUser = true;
                continue;
            }

            // Look for token: within the user section
            if (inUser && trimmed.StartsWith("token:", StringComparison.Ordinal))
            {
                var token = trimmed.Substring("token:".Length).Trim();
                if (!string.IsNullOrEmpty(token))
                {
                    return token;
                }
            }

            // Reset if we hit a new top-level section
            if (!string.IsNullOrEmpty(trimmed) && !char.IsWhiteSpace(line[0]) &&
                !trimmed.StartsWith("-", StringComparison.Ordinal) &&
                !trimmed.StartsWith("users:", StringComparison.Ordinal))
            {
                if (inUsers)
                {
                    break;
                }
            }
        }

        return null;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }
}

#region JSON Models for DCP API (AOT-compatible)

internal sealed class DcpExecutableRequest
{
    [JsonPropertyName("apiVersion")]
    public string? ApiVersion { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("metadata")]
    public DcpObjectMetadata? Metadata { get; set; }

    [JsonPropertyName("spec")]
    public DcpExecutableSpecRequest? Spec { get; set; }
}

internal sealed class DcpObjectMetadata
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("namespace")]
    public string? NamespaceProperty { get; set; }

    [JsonPropertyName("annotations")]
    public Dictionary<string, string>? Annotations { get; set; }
}

internal sealed class DcpExecutableSpecRequest
{
    [JsonPropertyName("executablePath")]
    public string? ExecutablePath { get; set; }

    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; set; }

    [JsonPropertyName("args")]
    public List<string>? Args { get; set; }

    [JsonPropertyName("env")]
    public List<DcpEnvVar>? Env { get; set; }
}

internal sealed class DcpEnvVar
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

internal sealed class DcpExecutableResponse
{
    [JsonPropertyName("metadata")]
    public DcpObjectMetadata? Metadata { get; set; }

    [JsonPropertyName("status")]
    public DcpExecutableStatusResponse? Status { get; set; }
}

internal sealed class DcpExecutableStatusResponse
{
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("pid")]
    public int? Pid { get; set; }

    [JsonPropertyName("stdOutFile")]
    public string? StdOutFile { get; set; }

    [JsonPropertyName("stdErrFile")]
    public string? StdErrFile { get; set; }
}

internal sealed class DcpServiceRequest
{
    [JsonPropertyName("apiVersion")]
    public string? ApiVersion { get; set; }

    [JsonPropertyName("kind")]
    public string? Kind { get; set; }

    [JsonPropertyName("metadata")]
    public DcpObjectMetadata? Metadata { get; set; }

    [JsonPropertyName("spec")]
    public DcpServiceSpecRequest? Spec { get; set; }
}

internal sealed class DcpServiceSpecRequest
{
    [JsonPropertyName("port")]
    public int? Port { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }

    [JsonPropertyName("protocol")]
    public string? Protocol { get; set; }

    [JsonPropertyName("addressAllocationMode")]
    public string? AddressAllocationMode { get; set; }
}

internal sealed class DcpServiceResponse
{
    [JsonPropertyName("metadata")]
    public DcpObjectMetadata? Metadata { get; set; }

    [JsonPropertyName("status")]
    public DcpServiceStatusResponse? Status { get; set; }
}

internal sealed class DcpServiceStatusResponse
{
    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("effectiveAddress")]
    public string? EffectiveAddress { get; set; }

    [JsonPropertyName("effectivePort")]
    public int? EffectivePort { get; set; }
}

/// <summary>
/// JSON serializer context for AOT compatibility.
/// </summary>
/// <summary>
/// Represents a service producer annotation entry for DCP.
/// </summary>
internal sealed class DcpServiceProducerAnnotation
{
    [JsonPropertyName("serviceName")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("address")]
    public string? Address { get; set; }
}

[JsonSerializable(typeof(DcpExecutableRequest))]
[JsonSerializable(typeof(DcpExecutableResponse))]
[JsonSerializable(typeof(DcpObjectMetadata))]
[JsonSerializable(typeof(DcpExecutableSpecRequest))]
[JsonSerializable(typeof(DcpExecutableStatusResponse))]
[JsonSerializable(typeof(DcpEnvVar))]
[JsonSerializable(typeof(DcpServiceRequest))]
[JsonSerializable(typeof(DcpServiceResponse))]
[JsonSerializable(typeof(DcpServiceSpecRequest))]
[JsonSerializable(typeof(DcpServiceStatusResponse))]
[JsonSerializable(typeof(List<string>))]
[JsonSerializable(typeof(List<DcpEnvVar>))]
[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(DcpServiceProducerAnnotation[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
internal sealed partial class DcpJsonContext : JsonSerializerContext
{
}

#endregion
