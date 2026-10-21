// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Nodes;
using Microsoft.Extensions.Configuration;

namespace Aspire.Cli.Configuration;

internal sealed class ConfigSourceConfigurationProvider : ConfigurationProvider, IConfigurationSource
{
    private static readonly HashSet<string> s_projectOwnedSettings = new(StringComparer.OrdinalIgnoreCase)
    {
        "$schema", "configSource", "appHost", "appHostPath", "sdk", "sdkVersion", "channel", "language", "profiles", "packages"
    };
    private readonly Dictionary<string, IDictionary<string, string?>> _projectDefaults = new(StringComparers.FileSystemPath);

    public IConfigurationProvider Build(IConfigurationBuilder builder) => this;

    private JsonObject _globalSettings = new();
    private HashSet<string> _fileKeys = new(StringComparer.OrdinalIgnoreCase);
    private bool _isActive;

    public void ConfigureLaunchSettings(JsonObject? globalSettings, JsonObject? localSettings)
    {
        _globalSettings = globalSettings?.DeepClone().AsObject() ?? new JsonObject();
        _fileKeys = Flatten(_globalSettings).Keys.Concat(Flatten(localSettings ?? new JsonObject()).Keys)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public void ClearConfiguration()
    {
        if (_isActive)
        {
            Data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            _isActive = false;
            OnReload();
        }
    }

    public void UseConfiguration(JsonObject configuration, string? configPath, JsonObject? projectSettings)
    {
        var shared = new JsonObject();
        foreach (var setting in configuration)
        {
            if (!s_projectOwnedSettings.Contains(setting.Key.Split(':')[0]))
            {
                shared[setting.Key] = setting.Value?.DeepClone();
            }
        }
        var effective = _globalSettings.DeepClone().AsObject();
        ApplyOverrides(effective, shared);
        if (projectSettings is not null)
        {
            Utils.ConfigurationHelper.NormalizeSettings(projectSettings);
            ApplyOverrides(effective, projectSettings);
            ApplyOverrides(shared, projectSettings);
        }

        Data = Flatten(effective);
        if (configPath is not null)
        {
            // Scoped lookups must finish any requested parent traversal before falling back to globals.
            _projectDefaults[configPath] = Flatten(shared);
        }
        _isActive = true;
        OnReload();
    }

    public override bool TryGet(string key, out string? value)
    {
        return base.TryGet(key, out value) || (_isActive && _fileKeys.Contains(key));
    }

    public override IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
    {
        // Mask inherited keys in enumeration as well as value lookup. Null placeholders
        // would still appear as extra array elements or settings when binding collections.
        var visibleKeys = _isActive
            ? earlierKeys.Where(key => !_fileKeys.Contains(parentPath is null ? key : $"{parentPath}:{key}"))
            : earlierKeys;

        return base.GetChildKeys(visibleKeys, parentPath);
    }

    public bool TryGetProjectDefault(string configPath, string key, out string? value)
    {
        value = null;
        return _projectDefaults.TryGetValue(configPath, out var defaults) && defaults.TryGetValue(key, out value);
    }

    private static void ApplyOverrides(JsonObject defaults, JsonObject overrides)
    {
        foreach (var setting in overrides)
        {
            var existingKey = defaults.Select(p => p.Key).FirstOrDefault(key => key.Equals(setting.Key, StringComparison.OrdinalIgnoreCase)) ?? setting.Key;
            if (defaults[existingKey] is JsonObject nested && setting.Value is JsonObject nestedOverrides)
            {
                ApplyOverrides(nested, nestedOverrides);
            }
            else
            {
                // Arrays and explicit nulls replace the whole inherited value, rather
                // than leaving stale child keys from the remote configuration behind.
                defaults[existingKey] = setting.Value?.DeepClone();
            }
        }
    }

    private static Dictionary<string, string?> Flatten(JsonObject configuration)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(configuration.ToJsonString()));
        using var defaults = new ConfigurationManager();
        defaults.AddJsonStream(stream);

        return defaults.AsEnumerable().ToDictionary(p => p.Key, p => p.Value, StringComparer.OrdinalIgnoreCase);
    }
}
