// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO.Hashing;
using System.Text;
using System.Text.Json;
using Aspire.Hosting.Utils;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A finalized URL through which a browser can access a resource endpoint.
/// </summary>
[AspireExport(ExposeProperties = true)]
public sealed class BrowserUrl
{
    internal BrowserUrl(Uri url, ResourceUrlAnnotation annotation)
    {
        Url = new UriBuilder(url) { Host = url.HostNameType == UriHostNameType.Dns ? url.IdnHost : url.Host }.Uri;
        Endpoint = annotation.Endpoint!;
        DisplayOrder = annotation.DisplayOrder ?? 0;
        DisplayLocation = annotation.DisplayLocation;
    }

    /// <summary>
    /// Gets the full browser URL, including its navigation path, query, and fragment.
    /// </summary>
    public Uri Url { get; }

    /// <summary>
    /// Gets the normalized origin, excluding credentials, navigation paths, queries, fragments, and default ports.
    /// </summary>
    /// <seealso href="https://url.spec.whatwg.org/#concept-origin"/>
    public string Origin => Url.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped);

    /// <summary>
    /// Gets the underlying endpoint. Its host and container network allocations are unchanged.
    /// </summary>
    public EndpointReference Endpoint { get; }

    /// <summary>
    /// Gets the display order used when selecting a preferred browser URL.
    /// </summary>
    public int DisplayOrder { get; }

    /// <summary>
    /// Gets the dashboard locations where the URL is displayed.
    /// </summary>
    public UrlDisplayLocation DisplayLocation { get; }
}

/// <summary>
/// References a resource's preferred browser origin independently of its physical endpoint allocations.
/// </summary>
/// <remarks>
/// URLs are available after URL customization completes. Preferred values follow dashboard ordering,
/// while collection projections include additional endpoint URLs and deduplicate normalized values.
/// Scalar resolution and interpolation select the preferred origin, not the full navigation URL or all origins.
/// Browser references do not change service discovery. Publishing requires an explicit publisher-supported
/// expression; local browser URLs are never substituted for deployed addresses.
/// </remarks>
/// <example>
/// <code>
/// var browserUrls = frontend.GetBrowserOrigin("https");
/// backend.WithEnvironment("FRONTEND_ORIGIN", browserUrls);
/// backend.WithEnvironment("ALLOWED_ORIGINS", browserUrls.Origins.Join(";"));
/// </code>
/// </example>
[AspireExport(ExposeProperties = true)]
public sealed class BrowserOriginReference : IExpressionValue, IValueWithReferences
{
    private readonly BrowserUrlSnapshotAnnotation _snapshot;
    private readonly string _name = "preferred-origin";
    private readonly Func<BrowserUrlValueContext, ValueTask<string?>>? _callback;
    private readonly ReferenceExpression? _publishExpression;
    private string? _manifestResourceName;

    /// <summary>
    /// Creates a reference to all browser URLs for a resource.
    /// </summary>
    /// <param name="resource">The resource whose browser URLs are referenced.</param>
    public BrowserOriginReference(IResourceWithEndpoints resource) : this(resource, null)
    {
    }

    /// <summary>
    /// Creates a reference to browser URLs for a resource, optionally restricted to one endpoint.
    /// </summary>
    /// <param name="resource">The resource whose browser URLs are referenced.</param>
    /// <param name="endpointName">The endpoint name, or <see langword="null"/> for all endpoints.</param>
    public BrowserOriginReference(IResourceWithEndpoints resource, string? endpointName)
    {
        ArgumentNullException.ThrowIfNull(resource);
        if (endpointName is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(endpointName);
        }
        Resource = resource;
        EndpointName = endpointName;
        _snapshot = BrowserUrlSnapshotAnnotation.GetOrCreate(resource);
    }

    private BrowserOriginReference(BrowserOriginReference reference, string name,
        Func<BrowserUrlValueContext, ValueTask<string?>>? callback, ReferenceExpression? publishExpression)
    {
        Resource = reference.Resource;
        EndpointName = reference.EndpointName;
        _snapshot = reference._snapshot;
        _name = name;
        _callback = callback;
        _publishExpression = publishExpression;
    }

    /// <summary>
    /// Gets the resource whose browser URLs are referenced.
    /// </summary>
    public IResourceWithEndpoints Resource { get; }

    /// <summary>
    /// Gets the endpoint filter, or <see langword="null"/> when all endpoints are included.
    /// </summary>
    public string? EndpointName { get; }

    /// <summary>
    /// Gets a scalar reference to the preferred full browser URL.
    /// </summary>
    public BrowserOriginReference PreferredUrl => CreateValue("preferred-url",
        context => ValueTask.FromResult<string?>(GetPreferred(context.Urls).Url.AbsoluteUri));

    /// <summary>
    /// Gets the reference to the manifest value resource representing this output configuration.
    /// </summary>
    /// <remarks>
    /// The manifest publisher emits a corresponding <c>value.v0</c> resource using the explicitly supplied
    /// publishing expression. Use <see cref="ToReferenceExpression"/> to supply an ordinary expression
    /// to other publishers.
    /// </remarks>
    public string ValueExpression => $"{{{ManifestResourceName}.connectionString}}";

    internal string ManifestResourceName
    {
        get
        {
            lock (_snapshot)
            {
                if (_manifestResourceName is not null)
                {
                    return _manifestResourceName;
                }

                // Expression text is only a naming hint: formats and custom provider instances can
                // change its meaning. Keep one stable token per reference, including through wrappers
                // and callbacks that read ValueExpression without a publishing context.
                var identity = $"{EndpointName ?? "<all>"}\n{_name}\n{_publishExpression?.ValueExpression}";
                var hash = XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(identity)).ToString("x16", CultureInfo.InvariantCulture);
                var baseName = $"browser-{Resource.Name}-{_name}-{hash}";
                var name = ResourceNameHelpers.TruncateWithHash(baseName, ModelName.DefaultMaxLength);
                var suffix = 1;
                while (!_snapshot.ManifestResourceNames.Add(name))
                {
                    name = ResourceNameHelpers.TruncateWithHash(
                        $"{baseName}-{suffix.ToString(CultureInfo.InvariantCulture)}", ModelName.DefaultMaxLength);
                    suffix++;
                }

                return _manifestResourceName = name;
            }
        }
    }

    /// <summary>
    /// Resolves the configured output after URL customization completes.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The preferred origin, or the explicitly configured URL, collection, or callback output.</returns>
    [AspireExport("BrowserOriginReference.getValueAsync", MethodName = "getValueAsync")]
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default) =>
        GetValueAsync(new ValueProviderContext(), cancellationToken);

    /// <summary>
    /// Resolves the configured output in the consumer's context.
    /// </summary>
    /// <param name="context">The consumer's resolution context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The configured runtime output, or the value of the explicit publishing expression.</returns>
    public async ValueTask<string?> GetValueAsync(ValueProviderContext context, CancellationToken cancellationToken)
    {
        if (context.ExecutionContext?.IsPublishMode == true)
        {
            return await ToReferenceExpression(context.ExecutionContext).GetValueAsync(context, cancellationToken).ConfigureAwait(false);
        }

        var urls = await GetUrlsAsync(context, cancellationToken).ConfigureAwait(false);
        return _callback is null
            ? GetPreferred(urls).Origin
            : await _callback(new BrowserUrlValueContext(this, urls, context, cancellationToken)).ConfigureAwait(false);
    }

    /// <summary>
    /// Supplies a publisher-supported expression without changing the configured runtime output.
    /// </summary>
    /// <param name="expression">The deployment expression produced by the integration or publisher.</param>
    /// <returns>A new browser reference with the same runtime output configuration and the supplied publishing expression.</returns>
    [AspireExport("BrowserOriginReference.withPublishExpression", MethodName = "withPublishExpression")]
    public BrowserOriginReference WithPublishExpression(ReferenceExpression expression)
    {
        ArgumentNullException.ThrowIfNull(expression);
        return new(this, _name, _callback, expression);
    }

    /// <summary>
    /// Creates an ordinary reference expression for the current run or publication operation.
    /// </summary>
    /// <param name="executionContext">The context in which the expression will be consumed.</param>
    /// <returns>
    /// A deferred browser value expression in run mode, or the explicitly configured deployment expression
    /// in publish/deploy mode. Publishing expressions contain their normal parameter/output references,
    /// not a browser value-provider wrapper.
    /// </returns>
    /// <exception cref="ArgumentNullException">The context is null.</exception>
    /// <exception cref="NotSupportedException">No expression was supplied for publication.</exception>
    /// <remarks>
    /// Call this when configuring the consuming value, after resource/environment configuration needed
    /// to construct the deployment expression. Native publishers use their existing ReferenceExpression support.
    /// </remarks>
    [AspireExport("BrowserOriginReference.toReferenceExpression", MethodName = "toReferenceExpression")]
    public ReferenceExpression ToReferenceExpression(DistributedApplicationExecutionContext executionContext)
    {
        ArgumentNullException.ThrowIfNull(executionContext);
        if (executionContext.IsRunMode)
        {
            return ReferenceExpression.Create($"{this}");
        }

        return _publishExpression ?? throw new NotSupportedException(
            $"Browser URL projection '{_name}' for resource '{Resource.Name}' requires a publisher-supported expression. Configure it with WithPublishExpression.");
    }

    /// <summary>
    /// Gets the normalized, distinct full browser URLs in preferred order.
    /// </summary>
    public BrowserUrlCollectionReference Urls => new(this, origins: false);

    /// <summary>
    /// Gets the normalized, distinct browser origins in preferred order.
    /// </summary>
    public BrowserUrlCollectionReference Origins => new(this, origins: true);

    /// <summary>
    /// Waits for finalized browser URLs, including their endpoint and display metadata.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The browser URLs in preferred order.</returns>
    [AspireExport("BrowserOriginReference.getUrlsAsync", MethodName = "getUrlsAsync")]
    public ValueTask<IReadOnlyList<BrowserUrl>> GetUrlsAsync(CancellationToken cancellationToken = default) =>
        GetUrlsAsync(new ValueProviderContext(), cancellationToken);

    /// <summary>
    /// Waits for finalized browser URLs in the supplied resolution context.
    /// </summary>
    /// <param name="context">The value resolution context.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The browser URLs in preferred order.</returns>
    /// <exception cref="NotSupportedException">Concrete browser URLs were requested during publishing.</exception>
    [AspireExportIgnore(Reason = "ValueProviderContext is a .NET consumer resolution context.")]
    public async ValueTask<IReadOnlyList<BrowserUrl>> GetUrlsAsync(ValueProviderContext context, CancellationToken cancellationToken)
    {
        if (context.ExecutionContext?.IsPublishMode == true)
        {
            throw new NotSupportedException("Concrete browser URLs cannot be resolved during publishing. Use a publisher-supported expression instead.");
        }

        if (!_snapshot.Value.IsValueSet && BrowserUrlSnapshotAnnotation.IsProcessingUrls.Value)
        {
            throw new InvalidOperationException("Browser URLs cannot be resolved from URL callbacks before their URLs have been finalized.");
        }

        var urls = await _snapshot.Value.GetValueAsync(cancellationToken).ConfigureAwait(false);
        return EndpointName is null ? urls : urls.Where(url =>
            string.Equals(url.Endpoint.EndpointName, EndpointName, StringComparisons.EndpointAnnotationName)).ToArray();
    }

    /// <summary>
    /// Creates a deferred scalar value from the finalized browser URLs.
    /// </summary>
    /// <param name="name">A name identifying this projection within the reference.</param>
    /// <param name="callback">The callback that converts browser URLs to consumer-specific configuration.</param>
    /// <returns>A value that can be used in environment variables and reference expressions.</returns>
    /// <remarks>
    /// The callback runs during value resolution, not during URL customization. For publication, use
    /// <see cref="WithPublishExpression"/> with an expression supplied by the integration or publisher.
    /// Collection-aware consumers can instead call <see cref="GetUrlsAsync(CancellationToken)"/> directly.
    /// </remarks>
    /// <example>
    /// <code>
    /// var redirects = frontend.GetBrowserOrigin().CreateValue("redirects", context =>
    ///     ValueTask.FromResult((string?)JsonSerializer.Serialize(
    ///         context.Urls.Select(url => url.Origin + "/signin-oidc"))));
    /// </code>
    /// </example>
    [AspireExportIgnore(Reason = "Consumer callbacks use .NET value provider contexts and may create publisher-native configuration.")]
    public BrowserOriginReference CreateValue(string name, Func<BrowserUrlValueContext, ValueTask<string?>> callback)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentNullException.ThrowIfNull(callback);
        ModelName.ValidateName(nameof(name), name);
        return new(this, name, callback, null);
    }

    private BrowserUrl GetPreferred(IReadOnlyList<BrowserUrl> urls) =>
        urls.Count > 0 ? urls[0] : throw new InvalidOperationException(
            $"No browser URL is available for resource '{Resource.Name}'{(EndpointName is null ? "." : $" and endpoint '{EndpointName}'.")}");

    IEnumerable<object> IValueWithReferences.References => _publishExpression is null
        ? [Resource]
        : [Resource, _publishExpression];
}

/// <summary>
/// A deferred collection of browser URLs or origins with explicit string projections.
/// </summary>
[AspireExport(ExposeMethods = true)]
public sealed class BrowserUrlCollectionReference
{
    private readonly BrowserOriginReference _source;
    private readonly bool _origins;

    internal BrowserUrlCollectionReference(BrowserOriginReference source, bool origins)
    {
        _source = source;
        _origins = origins;
    }

    /// <summary>
    /// Resolves normalized, distinct values in preferred order.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The full URLs or origins.</returns>
    public async ValueTask<IReadOnlyList<string>> GetValuesAsync(CancellationToken cancellationToken = default) =>
        GetValues(await _source.GetUrlsAsync(cancellationToken).ConfigureAwait(false));

    /// <summary>
    /// Joins the deferred collection using an explicitly chosen separator.
    /// </summary>
    /// <param name="separator">The separator required by the consumer.</param>
    /// <returns>A scalar reference suitable for environment variables or interpolation.</returns>
    public BrowserOriginReference Join(string separator)
    {
        ArgumentNullException.ThrowIfNull(separator);
        var separatorName = XxHash3.HashToUInt64(Encoding.UTF8.GetBytes(separator)).ToString("x", CultureInfo.InvariantCulture);
        var name = $"{(_origins ? "origins" : "urls")}-join-{separatorName}";
        return _source.CreateValue(name, context => ValueTask.FromResult<string?>(string.Join(separator, GetValues(context.Urls))));
    }

    /// <summary>
    /// Serializes the deferred collection as a JSON array of strings.
    /// </summary>
    /// <returns>A scalar JSON reference suitable for environment variables or interpolation.</returns>
    public BrowserOriginReference AsJson() =>
        _source.CreateValue(_origins ? "origins-json" : "urls-json",
            context => ValueTask.FromResult<string?>(JsonSerializer.Serialize(GetValues(context.Urls))));

    private string[] GetValues(IReadOnlyList<BrowserUrl> urls) =>
        urls.Select(url => _origins ? url.Origin : url.Url.AbsoluteUri).Distinct(StringComparer.Ordinal).ToArray();
}

/// <summary>
/// Supplies finalized browser URLs and consumer context to a deferred value callback.
/// </summary>
public sealed class BrowserUrlValueContext
{
    internal BrowserUrlValueContext(BrowserOriginReference reference, IReadOnlyList<BrowserUrl> urls, ValueProviderContext context, CancellationToken cancellationToken)
    {
        Reference = reference;
        Urls = urls;
        ValueProviderContext = context;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the source reference, including its resource and endpoint filter.
    /// </summary>
    public BrowserOriginReference Reference { get; }

    /// <summary>
    /// Gets the finalized browser URLs in preferred order.
    /// </summary>
    public IReadOnlyList<BrowserUrl> Urls { get; }

    /// <summary>
    /// Gets the consumer's resolution context.
    /// </summary>
    public ValueProviderContext ValueProviderContext { get; }

    /// <summary>
    /// Gets the cancellation token for this resolution.
    /// </summary>
    public CancellationToken CancellationToken { get; }
}
