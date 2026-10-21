// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Hosting.ApplicationModel;

internal sealed class BrowserUrlSnapshotAnnotation : IResourceAnnotation
{
    internal static AsyncLocal<bool> IsProcessingUrls { get; } = new();

    public ValueSnapshot<IReadOnlyList<BrowserUrl>> Value { get; } = new();

    // Accessed under the snapshot lock when a browser reference first exposes its manifest token.
    internal HashSet<string> ManifestResourceNames { get; } = new(StringComparers.ResourceName);

    public static BrowserUrlSnapshotAnnotation GetOrCreate(IResource resource)
    {
        lock (resource.Annotations)
        {
            if (resource.TryGetLastAnnotation<BrowserUrlSnapshotAnnotation>(out var snapshot))
            {
                return snapshot;
            }

            snapshot = new BrowserUrlSnapshotAnnotation();
            resource.Annotations.Add(snapshot);
            return snapshot;
        }
    }

    public void Capture(IResource resource, IEnumerable<ResourceUrlAnnotation> urls)
    {
        var candidates = new List<BrowserUrl>();
        var resourceComparer = new ResourceNameComparer();
        foreach (var url in urls)
        {
            // A resource can display another resource's endpoint or unrelated documentation links.
            // Neither contributes an address for this resource's browser references.
            if (url.Endpoint is not { } endpoint || !resourceComparer.Equals(endpoint.Resource, resource))
            {
                continue;
            }

            if (!Uri.TryCreate(url.Url, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"The browser URL '{url.Url}' for resource '{resource.Name}' must be absolute.");
            }

            if (uri.Scheme is not "http" and not "https")
            {
                continue;
            }

            candidates.Add(new BrowserUrl(uri, url));
        }

        // Match ResourceUrlHelpers.GetUrls ordering, preferring summary links over detail-only aliases.
        // Do not require active/listening URLs: consumers need these values before workloads start.
        Value.SetValue(candidates
            .OrderBy(url => url.DisplayLocation == UrlDisplayLocation.DetailsOnly)
            .ThenByDescending(url => url.DisplayOrder)
            .ThenByDescending(url => url.Url.Scheme == Uri.UriSchemeHttps)
            .ThenBy(url => url.Endpoint.EndpointName, StringComparers.EndpointAnnotationName)
            .ToImmutableArray());
    }
}
