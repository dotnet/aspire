// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Microsoft.DotNet.ProjectTools;

public static class LaunchSettings
{
    private const string ProfilesKey = "profiles";
    private const string CommandNameKey = "commandName";

    private static readonly IReadOnlyDictionary<string, LaunchProfileParser> s_providers = new Dictionary<string, LaunchProfileParser>
    {
        { ProjectLaunchProfileParser.CommandName, ProjectLaunchProfileParser.Instance },
        { ExecutableLaunchProfileParser.CommandName, ExecutableLaunchProfileParser.Instance }
    };

    public static IEnumerable<string> SupportedProfileTypes => s_providers.Keys;

    public static string GetPropertiesLaunchSettingsPath(string directoryPath, string propertiesDirectoryName)
        => Path.Combine(directoryPath, propertiesDirectoryName, "launchSettings.json");

    public static string GetFlatLaunchSettingsPath(string directoryPath, string projectNameWithoutExtension)
        => Path.Join(directoryPath, $"{projectNameWithoutExtension}.run.json");

    public static string? TryFindLaunchSettingsFile(string projectOrEntryPointFilePath, string? launchProfile, Action<string, bool> report)
    {
        var buildPathContainer = Path.GetDirectoryName(projectOrEntryPointFilePath);
        Debug.Assert(buildPathContainer != null);

        // VB.NET projects store the launch settings file in the
        // "My Project" directory instead of a "Properties" directory.
        // TODO: use the `AppDesignerFolder` MSBuild property instead, which captures this logic already
        var propsDirectory = string.Equals(Path.GetExtension(projectOrEntryPointFilePath), ".vbproj", StringComparison.OrdinalIgnoreCase)
             ? "My Project"
             : "Properties";

        string launchSettingsPath = GetPropertiesLaunchSettingsPath(buildPathContainer, propsDirectory);
        bool hasLaunchSetttings = File.Exists(launchSettingsPath);

        string appName = Path.GetFileNameWithoutExtension(projectOrEntryPointFilePath);
        string runJsonPath = GetFlatLaunchSettingsPath(buildPathContainer, appName);
        bool hasRunJson = File.Exists(runJsonPath);

        if (hasLaunchSetttings)
        {
            if (hasRunJson)
            {
                report(string.Format(Resources.RunCommandWarningRunJsonNotUsed, runJsonPath, launchSettingsPath), false);
            }

            return launchSettingsPath;
        }

        if (hasRunJson)
        {
            return runJsonPath;
        }

        if (!string.IsNullOrEmpty(launchProfile))
        {
            report(string.Format(Resources.RunCommandExceptionCouldNotLocateALaunchSettingsFile, launchProfile, $"""
                    {launchSettingsPath}
                    {runJsonPath}
                    """), true);
        }

        return null;
    }

    public static LaunchProfileParseResult ReadProfileSettingsFromFile(string launchSettingsPath, string? profileName = null)
    {
        try
        {
            var launchSettingsJsonContents = File.ReadAllText(launchSettingsPath);

            var jsonDocumentOptions = new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            };

            using (var document = JsonDocument.Parse(launchSettingsJsonContents, jsonDocumentOptions))
            {
                var model = document.RootElement;

                if (model.ValueKind != JsonValueKind.Object || !model.TryGetProperty(ProfilesKey, out var profilesObject) || profilesObject.ValueKind != JsonValueKind.Object)
                {
                    return LaunchProfileParseResult.Failure(Resources.LaunchProfilesCollectionIsNotAJsonObject);
                }

                var selectedProfileName = profileName;
                JsonElement profileObject;
                if (string.IsNullOrEmpty(profileName))
                {
                    var firstProfileProperty = profilesObject.EnumerateObject().FirstOrDefault(IsDefaultProfileType);
                    selectedProfileName = firstProfileProperty.Value.ValueKind == JsonValueKind.Object ? firstProfileProperty.Name : null;
                    profileObject = firstProfileProperty.Value;
                }
                else // Find a profile match for the given profileName
                {
                    IEnumerable<JsonProperty> caseInsensitiveProfileMatches = [.. profilesObject
                        .EnumerateObject() // p.Name shouldn't fail, as profileObject enumerables here are only created from an existing JsonObject
                        .Where(p => string.Equals(p.Name, profileName, StringComparison.OrdinalIgnoreCase))];

                    if (caseInsensitiveProfileMatches.Count() > 1)
                    {
                        return LaunchProfileParseResult.Failure(string.Format(Resources.DuplicateCaseInsensitiveLaunchProfileNames,
                            string.Join(",\n", caseInsensitiveProfileMatches.Select(p => $"\t{p.Name}"))));
                    }

                    if (!caseInsensitiveProfileMatches.Any())
                    {
                        return LaunchProfileParseResult.Failure(string.Format(Resources.LaunchProfileDoesNotExist, profileName));
                    }

                    profileObject = profilesObject.GetProperty(caseInsensitiveProfileMatches.First().Name);

                    if (profileObject.ValueKind != JsonValueKind.Object)
                    {
                        return LaunchProfileParseResult.Failure(Resources.LaunchProfileIsNotAJsonObject);
                    }
                }

                if (profileObject.ValueKind == default)
                {
                    foreach (var prop in profilesObject.EnumerateObject())
                    {
                        if (prop.Value.ValueKind == JsonValueKind.Object)
                        {
                            if (prop.Value.TryGetProperty(CommandNameKey, out var commandNameElement) && commandNameElement.ValueKind == JsonValueKind.String)
                            {
                                if (commandNameElement.GetString() is { } commandNameElementKey && s_providers.ContainsKey(commandNameElementKey))
                                {
                                    profileObject = prop.Value;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (profileObject.ValueKind == default)
                {
                    return LaunchProfileParseResult.Failure(Resources.UsableLaunchProfileCannotBeLocated);
                }

                if (!profileObject.TryGetProperty(CommandNameKey, out var finalCommandNameElement)
                    || finalCommandNameElement.ValueKind != JsonValueKind.String)
                {
                    return LaunchProfileParseResult.Failure(Resources.UsableLaunchProfileCannotBeLocated);
                }

                string? commandName = finalCommandNameElement.GetString();
                if (!TryLocateHandler(commandName, out LaunchProfileParser? provider))
                {
                    return LaunchProfileParseResult.Failure(string.Format(Resources.LaunchProfileHandlerCannotBeLocated, commandName));
                }

                return provider.ParseProfile(launchSettingsPath, selectedProfileName, profileObject.GetRawText());
            }
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            return LaunchProfileParseResult.Failure(string.Format(Resources.DeserializationExceptionMessage, launchSettingsPath, ex.Message));
        }
    }

    private static bool TryLocateHandler(string? commandName, [NotNullWhen(true)] out LaunchProfileParser? provider)
    {
        if (commandName == null)
        {
            provider = null;
            return false;
        }

        return s_providers.TryGetValue(commandName, out provider);
    }

    private static bool IsDefaultProfileType(JsonProperty profileProperty)
    {
        if (profileProperty.Value.ValueKind != JsonValueKind.Object
            || !profileProperty.Value.TryGetProperty(CommandNameKey, out var commandNameElement)
            || commandNameElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var commandName = commandNameElement.GetString();
        return commandName != null && s_providers.ContainsKey(commandName);
    }
}
