// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Lifecycle;

namespace Aspire.Hosting;

internal sealed class SettingsFileWriterHook : IDistributedApplicationLifecycleHook
{
    public async Task AfterEndpointsAllocatedAsync(DistributedApplicationModel appModel, CancellationToken cancellationToken)
    {
        var projectResources = appModel.Resources.OfType<ProjectResource>();

        foreach (var projectResource in projectResources)
        {
            if (projectResource.TryGetLastAnnotation<SettingsFileAnnotation>(out var settingsFileAnnotation))
            {
                var settingsFileOptions = settingsFileAnnotation.SettingsFileOptions;

                var config = new Dictionary<string, object>();
                DistributedApplicationExecutionContext? executionContext = null; // TODO: How to get this?
                var context = new EnvironmentCallbackContext(executionContext!, config, cancellationToken)
                {
                    //Logger = resourceLogger
                };

                if (projectResource.TryGetEnvironmentVariables(out var envVarAnnotations))
                {
                    foreach (var ann in envVarAnnotations)
                    {
                        await ann.Callback(context).ConfigureAwait(false);
                    }
                }

                List<Setting> settings = new List<Setting>();
                foreach (var c in config)
                {
                    try
                    {
                        var value = c.Value switch
                        {
                            string s => s,
                            //IValueProvider valueProvider => await GetValue(c.Key, valueProvider, resourceLogger, isContainer: false, cancellationToken).ConfigureAwait(false),
                            null => null,
                            _ => throw new InvalidOperationException($"Unexpected value for environment variable \"{c.Key}\".")
                        };

                        if (value is not null)
                        {
                            settings.Add(new Setting(c.Key, value));
                        }
                    }
                    catch (Exception)
                    {
                        //resourceLogger.LogCritical("Failed to apply configuration value '{ConfigKey}'. A dependency may have failed to start.", c.Key);
                        //_logger.LogDebug(ex, "Failed to apply configuration value '{ConfigKey}'. A dependency may have failed to start.", c.Key);
                        //failedToApplyConfiguration = true;
                    }
                }

                if (settingsFileOptions.SettingsFileType == SettingsFileType.CSharp)
                {
                    WriteCSharpSettings(settings, settingsFileOptions.SettingsFilePath!);
                }
                else
                {
                    //WriteJsonSettings(projectResource.EnvironmentVariables, settingsFileOptions.SettingsFilePath!);
                }
            }
        }
    }

    static void WriteCSharpSettings(List<Setting> settings, string settingsPath)
    {
        var prefixesToRemove = new List<string>
        {
            "ASPNETCORE_",
            "DOTNET_",
        };

        // Get the subset of variables that are provided by Aspire and sort them
        List<Setting> transformedSettings = new List<Setting>();
        foreach (var setting in settings)
        {
            string variableName = setting.Name;
            string value = setting.Value;

            // Normalize the key, matching the logic here:
            // https://github.dev/dotnet/runtime/blob/main/src/libraries/Microsoft.Extensions.Configuration.EnvironmentVariables/src/EnvironmentVariablesConfigurationProvider.cs
            variableName = variableName.Replace("__", ":");

            // For defined prefixes, add the variable with the prefix removed, matching the logic
            // in EnvironmentVariablesConfigurationProvider.cs. Also add the variable with the
            // prefix intact, which matches the normal HostApplicationBuilder behavior, where
            // there's an EnvironmentVariablesConfigurationProvider added with and another one
            // without the prefix set.
            foreach (var prefix in prefixesToRemove)
            {
                if (variableName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    transformedSettings.Add(new Setting(variableName, value));
                    variableName = variableName.Substring(prefix.Length);
                    break;
                }
            }

            transformedSettings.Add(new Setting(variableName, value));
        }

        transformedSettings.Sort();

        // TODO: Should we add a UTF-8 BOM or escape non-ASCII characters in setting values?
        using (var file = new StreamWriter(settingsPath))
        {
            EnsureFileIsReadable(settingsPath);

            file.Write("""
                    // This file is generated from the Aspire AppHost project. Rerun the Aspire AppHost
                    // to regenerate it.
                    
                    public static class AspireAppSettings
                    {
                        public static readonly Dictionary<string, string> Settings =
                            new Dictionary<string, string>
                            {

                    """);

            foreach (Setting setting in transformedSettings)
            {
                var value = setting.Value;
                string escapedValue;
                if (value.Contains('"') || value.Contains('\\'))
                {
                    escapedValue = "@\"" + value.Replace("\"", "\"\"") + "\"";
                }
                else
                {
                    escapedValue = "\"" + value + "\"";
                }

                file.WriteLine($"""            ["{setting.Name}"] = {escapedValue},""");
            }

            file.Write("""
                            };
                    }
                    """);
        }
    }

    private static void EnsureFileIsReadable(string filePath)
    {
        // Need to grant read access to the config file on unix like systems.
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.GroupRead | UnixFileMode.OtherRead);
        }
    }

    private class Setting(string name, string value) : IComparable<Setting>
    {
        public string Name { get; } = name;
        public string Value { get; } = value;

        public string GetCSharpEscapedValue()
        {
            // If the string contains a quote or escape sequence, use a verbatim string literal, where "" is used for "
            if (Value.Contains('"') || Value.Contains('\\'))
            {
                return "@\"" + Value.Replace("\"", "\"\"") + "\"";
            }
            else
            {
                return "\"" + Value + "\"";
            }
        }

        public int CompareTo(Setting? other) =>
            other is null ? 1 : Name.CompareTo(other.Name);
    }
}
