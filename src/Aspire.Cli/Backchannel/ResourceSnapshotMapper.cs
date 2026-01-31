// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Utils;
using Aspire.Shared.Model;
using Aspire.Shared.Model.Serialization;

namespace Aspire.Cli.Backchannel;

/// <summary>
/// Maps <see cref="ResourceSnapshot"/> to <see cref="ResourceJson"/> for serialization.
/// </summary>
internal static class ResourceSnapshotMapper
{
    /// <summary>
    /// Maps a list of <see cref="ResourceSnapshot"/> to a list of <see cref="ResourceJson"/>.
    /// </summary>
    /// <param name="snapshots">The resource snapshots to map.</param>
    /// <param name="dashboardBaseUrl">Optional base URL of the Aspire Dashboard for generating resource URLs.</param>
    public static List<ResourceJson> MapToResourceJsonList(IEnumerable<ResourceSnapshot> snapshots, string? dashboardBaseUrl = null)
    {
        var snapshotList = snapshots.ToList();
        return snapshotList.Select(s => MapToResourceJson(s, snapshotList, dashboardBaseUrl)).ToList();
    }

    /// <summary>
    /// Maps a <see cref="ResourceSnapshot"/> to <see cref="ResourceJson"/>.
    /// </summary>
    /// <param name="snapshot">The resource snapshot to map.</param>
    /// <param name="allSnapshots">All resource snapshots for resolving relationships.</param>
    /// <param name="dashboardBaseUrl">Optional base URL of the Aspire Dashboard for generating resource URLs.</param>
    public static ResourceJson MapToResourceJson(ResourceSnapshot snapshot, IReadOnlyList<ResourceSnapshot> allSnapshots, string? dashboardBaseUrl = null)
    {
        var urls = snapshot.Endpoints
            .Select(e => new ResourceUrlJson
            {
                Name = e.Name,
                Url = e.Url,
                IsInternal = e.IsInternal
            })
            .ToArray();

        var volumes = snapshot.Volumes
            .Select(v => new ResourceVolumeJson
            {
                Source = v.Source,
                Target = v.Target,
                MountType = v.MountType,
                IsReadOnly = v.IsReadOnly
            })
            .ToArray();

        var healthReports = snapshot.HealthReports
            .Select(h => new ResourceHealthReportJson
            {
                Name = h.Name,
                Status = h.Status,
                Description = h.Description,
                ExceptionMessage = h.ExceptionText
            })
            .ToArray();

        var properties = snapshot.Properties
            .Select(p => new ResourcePropertyJson
            {
                Name = p.Key,
                Value = p.Value
            })
            .ToArray();

        // Build relationships by matching DisplayName
        var relationships = new List<ResourceRelationshipJson>();
        foreach (var relationship in snapshot.Relationships)
        {
            var matches = allSnapshots
                .Where(r => string.Equals(r.DisplayName, relationship.ResourceName, StringComparisons.ResourceName))
                .ToList();

            foreach (var match in matches)
            {
                relationships.Add(new ResourceRelationshipJson
                {
                    Type = relationship.Type,
                    ResourceName = match.Name
                });
            }
        }

        // Only include enabled commands
        var commands = snapshot.Commands
            .Where(c => string.Equals(c.State, "Enabled", StringComparison.OrdinalIgnoreCase))
            .Select(c => new ResourceCommandJson
            {
                Name = c.Name,
                Description = c.Description
            })
            .ToArray();

        // Get source information using the shared ResourceSourceViewModel
        var sourceViewModel = ResourceSource.GetSourceModel(snapshot.ResourceType, snapshot.Properties);

        // Generate dashboard URL for this resource if a base URL is provided
        string? dashboardUrl = null;
        if (!string.IsNullOrEmpty(dashboardBaseUrl))
        {
            var resourcePath = DashboardUrls.ResourcesUrl(snapshot.Name);
            dashboardUrl = DashboardUrls.CombineUrl(dashboardBaseUrl, resourcePath);
        }

        return new ResourceJson
        {
            Name = snapshot.Name,
            ResourceType = snapshot.ResourceType,
            State = snapshot.State,
            StateStyle = snapshot.StateStyle,
            HealthStatus = snapshot.HealthStatus,
            Source = sourceViewModel?.Value,
            ExitCode = snapshot.ExitCode,
            CreationTimestamp = snapshot.CreatedAt,
            StartTimestamp = snapshot.StartedAt,
            StopTimestamp = snapshot.StoppedAt,
            DashboardUrl = dashboardUrl,
            Urls = urls,
            Volumes = volumes,
            HealthReports = healthReports,
            Properties = properties,
            Relationships = relationships.ToArray(),
            Commands = commands
        };
    }

    /// <summary>
    /// Gets the display name for a resource, returning the unique name if there are multiple resources
    /// with the same display name (replicas).
    /// </summary>
    /// <param name="resource">The resource to get the name for.</param>
    /// <param name="allResources">All resources to check for duplicates.</param>
    /// <returns>The display name if unique, otherwise the unique resource name.</returns>
    public static string GetResourceName(ResourceSnapshot resource, IDictionary<string, ResourceSnapshot> allResources)
    {
        return GetResourceName(resource, allResources.Values);
    }

    /// <summary>
    /// Gets the display name for a resource, returning the unique name if there are multiple resources
    /// with the same display name (replicas).
    /// </summary>
    /// <param name="resource">The resource to get the name for.</param>
    /// <param name="allResources">All resources to check for duplicates.</param>
    /// <returns>The display name if unique, otherwise the unique resource name.</returns>
    public static string GetResourceName(ResourceSnapshot resource, IEnumerable<ResourceSnapshot> allResources)
    {
        var count = 0;
        foreach (var item in allResources)
        {
            // Skip hidden resources
            if (string.Equals(item.State, "Hidden", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(item.DisplayName, resource.DisplayName, StringComparisons.ResourceName))
            {
                count++;
                if (count >= 2)
                {
                    // There are multiple resources with the same display name so they're part of a replica set.
                    // Need to use the name which has a unique ID to tell them apart.
                    return resource.Name;
                }
            }
        }

        return resource.DisplayName ?? resource.Name;
    }
}
