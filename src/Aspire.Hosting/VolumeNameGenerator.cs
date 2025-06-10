// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Utility class for generating volume names.
/// </summary>
public static class VolumeNameGenerator
{
    /// <summary>
    /// Generates a volume name with the form <c>{applicationName}-{sha256 of apphost path}-{resourceName}-{suffix}</c>, e.g. <c>myapplication-a345f2451-postgres-data</c>.
    /// </summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="builder">The resource builder.</param>
    /// <param name="suffix">The suffix to append to the volume name.</param>
    /// <returns>The volume name.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static string Generate<T>(IResourceBuilder<T> builder, string suffix) where T : IResource
    {
        if (!HasOnlyValidChars(suffix))
        {
            throw new ArgumentException($"The suffix '{suffix}' contains invalid characters. It must match [a-zA-Z0-9][a-zA-Z0-9_.-]*.", nameof(suffix));
        }

        // Create volume name like "{Sanitize(appname).Lower()}-{sha256.Lower()}-postgres-data"

        // Compute a short hash of the content root path to differentiate between multiple AppHost projects with similar volume names
        var safeApplicationName = Sanitize(builder.ApplicationBuilder.Environment.ApplicationName).ToLowerInvariant();
        var applicationHash = builder.ApplicationBuilder.Configuration["AppHost:Sha256"]![..10].ToLowerInvariant();
        var resourceName = builder.Resource.Name;
        return $"{safeApplicationName}-{applicationHash}-{resourceName}-{suffix}";
    }

    internal static string Sanitize(string name)
    {
        return string.Create(name.Length, name, static (s, name) =>
        {
            // According to the error message from docker CLI, volume names must be of form "[a-zA-Z0-9][a-zA-Z0-9_.-]"
            var nameSpan = name.AsSpan();

            for (var i = 0; i < nameSpan.Length; i++)
            {
                var c = nameSpan[i];

                s[i] = IsValidChar(i, c) ? c : '_';
            }
        });
    }

    private static bool HasOnlyValidChars(string value)
    {
        for (var i = 0; i < value.Length; i++)
        {
            if (!IsValidChar(i, value[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool IsValidChar(int i, char c)
    {
        if (i == 0 && !(char.IsAsciiLetter(c) || char.IsNumber(c)))
        {
            // First char must be a letter or number
            return false;
        }
        else if (!(char.IsAsciiLetter(c) || char.IsNumber(c) || c == '_' || c == '.' || c == '-'))
        {
            // Subsequent chars must be a letter, number, underscore, period, or hyphen
            return false;
        }

        return true;
    }
}
