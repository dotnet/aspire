// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using k8s;
using k8s.Autorest;

namespace Aspire.Hosting.Dcp;

// We need to create a custom Kubernetes client to support reading arbitrary subresources from a Kubernetes resource as a stream.
// k8s.Kubernetes does not support this operation natively, and required machinery (SendRequest() in particular) is protected.

internal sealed class DcpKubernetesClient : k8s.Kubernetes
{
    public DcpKubernetesClient(KubernetesClientConfiguration config, params DelegatingHandler[] handlers) : base(config, handlers)
    {
    }

    /// <summary>
    /// Asynchronously reads a sub-resource from a Kubernetes resource as a stream.
    /// </summary>
    /// <param name="group">The API group of the Kubernetes resource.</param>
    /// <param name="version">The API version of the Kubernetes resource.</param>
    /// <param name="plural">The plural name (API kind) of the Kubernetes resource, e.g. "executables".</param>
    /// <param name="name">The name of the Kubernetes resource to use for sub-resource read operation.</param>
    /// <param name="subResource">The sub-resource to read from the Kubernetes resource.</param>
    /// <param name="namespaceParameter">The namespace of the Kubernetes resource.
    /// If null or empty, the resource is assumed to be non-namespaced.</param>
    /// <param name="queryParams">Optional query parameters to append to the request URL.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    public async Task<HttpOperationResponse<Stream>> ReadSubResourceAsStreamAsync(
        string group,
        string version,
        string plural,
        string name,
        string subResource,
        string? namespaceParameter,
        IReadOnlyCollection<(string name, string value)>? queryParams = null,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group, nameof(group));
        ArgumentException.ThrowIfNullOrWhiteSpace(version, nameof(version));
        ArgumentException.ThrowIfNullOrWhiteSpace(plural, nameof(plural));
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentException.ThrowIfNullOrWhiteSpace(subResource, nameof(subResource));

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(HttpClientTimeout);
        cancellationToken = cts.Token;

        string url;
        if (string.IsNullOrEmpty(namespaceParameter))
        {
            url = $"apis/{group}/{version}/{plural}/{name}/{subResource}";
        }
        else
        {
            url = $"apis/{group}/{version}/namespaces/{namespaceParameter}/{plural}/{name}/{subResource}";
        }

        var q = new QueryBuilder();
        if (queryParams != null)
        {
            foreach (var (param, paramVal) in queryParams)
            {
                q.Append(param, paramVal);
            }
        }
        url += q.ToString();

        var httpResponse = await SendRequest<object?>(url, HttpMethod.Get, customHeaders : null, body: null, cancellationToken).ConfigureAwait(false);
        var httpRequest = httpResponse.RequestMessage;
        var result = new HttpOperationResponse<Stream>()
        {
            Request = httpRequest,
            Response = httpResponse,
            Body = await httpResponse.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false)
        };
        return result;
    }

    private sealed class QueryBuilder
    {
        private readonly List<string> _parameters = new List<string>();

        public void Append(string key, int val)
        {
            _parameters.Add($"{key}={val}");
        }

        public void Append(string key, bool? val)
        {
            _parameters.Add($"{key}={(val == true ? "true" : "false")}");
        }

        public void Append(string key, string val)
        {
            _parameters.Add($"{key}={Uri.EscapeDataString(val)}");
        }

        public override string ToString()
        {
            if (_parameters.Count > 0)
            {
                return $"?{string.Join("&", _parameters)}";
            }
            else
            {
                return string.Empty;
            }
        }
    }
}
