// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Aspire.Cli.Resources;

namespace Aspire.Cli.Agents;

/// <summary>
/// Registers the official marketplace without invoking plugin CLIs or changing installation state.
/// </summary>
internal static class AspireSkillsPluginConfiguration
{
    internal const string MarketplaceName = "aspire-skills";
    internal const string PluginName = "aspire@aspire-skills";
    internal const string Repository = "microsoft/aspire-skills";
    internal const string RepositoryUrl = "https://github.com/" + Repository;

    public static AgentConfigurationEdit Apply(JsonObject root, IReadOnlyList<JsonObject> settings)
    {
        JsonObject? selectedSource = null;
        foreach (var config in settings.Append(root))
        {
            var plugins = AgentConfigurationJson.OptionalObject(config, "enabledPlugins");
            if (plugins is not null && AgentConfigurationJson.Boolean(plugins, PluginName) is false)
            {
                return AgentConfigurationEdit.Skipped(AgentCommandStrings.Configuration_Disabled);
            }

            var marketplaces = AgentConfigurationJson.OptionalObject(config, "extraKnownMarketplaces");
            if (marketplaces is not null && marketplaces.TryGetPropertyValue(MarketplaceName, out var marketplaceNode))
            {
                if (marketplaceNode is not JsonObject marketplace ||
                    AgentConfigurationJson.OptionalObject(marketplace, "source") is not { } source)
                {
                    throw AgentConfigurationJson.Shape($"extraKnownMarketplaces.{MarketplaceName}.source");
                }

                if (!IsOfficialSource(source))
                {
                    return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_Conflict);
                }

                // Keep inherited release/commit pins unless the destination declares its own source.
                if (ReferenceEquals(config, root) || source.ContainsKey("ref") || source.ContainsKey("sha") || selectedSource is null)
                {
                    selectedSource = source;
                }
            }
        }

        selectedSource ??= new JsonObject
        {
            ["source"] = "github",
            ["repo"] = Repository
        };
        if (settings.Append(root).Any(config => HasMarketplacePolicyConflict(config, selectedSource)))
        {
            return AgentConfigurationEdit.Blocked(AgentCommandStrings.Configuration_PolicyBlocked);
        }

        var targetMarketplaces = AgentConfigurationJson.Object(root, "extraKnownMarketplaces");
        if (!targetMarketplaces.ContainsKey(MarketplaceName))
        {
            targetMarketplaces[MarketplaceName] = new JsonObject
            {
                ["source"] = selectedSource.DeepClone()
            };
        }

        // No autoUpdate opt-in and no installedPlugins/cache metadata: registration is not
        // acquisition. Existing entries, including explicit false, were checked above.
        var targetPlugins = AgentConfigurationJson.Object(root, "enabledPlugins");
        if (!targetPlugins.ContainsKey(PluginName))
        {
            targetPlugins[PluginName] = true;
        }

        return AgentConfigurationEdit.Applied();
    }

    private static bool IsOfficialSource(JsonObject source)
    {
        return AgentConfigurationJson.String(source["source"]) switch
        {
            "github" => string.Equals(AgentConfigurationJson.String(source["repo"]), Repository, StringComparison.OrdinalIgnoreCase),
            "git" => AgentConfigurationJson.String(source["url"]) is { } url &&
                (string.Equals(url.TrimEnd('/'), $"https://github.com/{Repository}", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(url.TrimEnd('/'), $"https://github.com/{Repository}.git", StringComparison.OrdinalIgnoreCase)),
            _ => false
        };
    }

    private static bool HasMarketplacePolicyConflict(JsonObject config, JsonObject source)
    {
        // Claude's managed marketplace lists match source objects, not marketplace display
        // names. Strict matching includes the source form, ref and path: a policy permitting
        // a pinned release does not permit our unpinned default. Unknown matchers remain blocked.
        // https://code.claude.com/docs/en/settings-reference#exact-matching
        if (config.TryGetPropertyValue("strictKnownMarketplaces", out var allowedNode) &&
            (allowedNode is not JsonArray allowed || !allowed.Any(entry => JsonNode.DeepEquals(entry, source))))
        {
            return true;
        }

        if (config.TryGetPropertyValue("blockedMarketplaces", out var blockedNode) &&
            (blockedNode is not JsonArray blocked || blocked.Any(entry => entry is not JsonObject blockedSource ||
                AgentConfigurationJson.String(blockedSource["source"]) is not ("github" or "git") ||
                MatchesBlockedSource(blockedSource, source))))
        {
            return true;
        }

        return false;
    }

    private static bool MatchesBlockedSource(JsonObject blocked, JsonObject source)
    {
        // An unpinned deny entry covers every ref; explicit refs and paths narrow that denial.
        // https://code.claude.com/docs/en/settings-reference#blockedmarketplaces
        return IsOfficialSource(blocked) && new[] { "ref", "path" }.All(key =>
            !blocked.TryGetPropertyValue(key, out var value) || JsonNode.DeepEquals(value, source[key]));
    }
}
