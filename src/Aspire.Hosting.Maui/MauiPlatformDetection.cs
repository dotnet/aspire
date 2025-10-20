// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Linq;

namespace Aspire.Hosting.Maui;

/// <summary>
/// Helper responsible for reading target frameworks from a MAUI project and selecting platforms for auto-detection.
/// </summary>
internal static class MauiPlatformDetection
{
    /// <summary>
    /// Loads all target frameworks (single and multi) declared in the project file.
    /// </summary>
    public static HashSet<string> LoadTargetFrameworks(string projectPath)
    {
        var doc = XDocument.Load(projectPath);
        var ns = doc.Root?.Name.Namespace ?? XNamespace.None;
        var list = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var tf in doc.Descendants(ns + "TargetFramework").Select(e => e.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)))
        {
            foreach (var t in tf)
            {
                list.Add(t.Trim());
            }
        }
        foreach (var tfs in doc.Descendants(ns + "TargetFrameworks").Select(e => e.Value.Split(';', StringSplitOptions.RemoveEmptyEntries)))
        {
            foreach (var t in tfs)
            {
                list.Add(t.Trim());
            }
        }
        return list;
    }

    /// <summary>
    /// Determines which platforms should be auto-detected based on the current host OS and available TFMs.
    /// Calls <paramref name="tryAdd"/> for each candidate; if it returns true, the platform is considered added.
    /// </summary>
    public static List<string> AutoDetect(HashSet<string> availableTfms, Func<string, bool> tryAdd)
    {
        var added = new List<string>();

        if (OperatingSystem.IsWindows())
        {
            Try("windows");
            Try("android");
        }
        else if (OperatingSystem.IsMacOS())
        {
            Try("maccatalyst");
            Try("ios");
            Try("android");
        }

        void Try(string moniker)
        {
            if (availableTfms.Any(t => t.Contains('-') && t.Split('-')[1].StartsWith(moniker, StringComparison.OrdinalIgnoreCase)))
            {
                if (tryAdd(moniker))
                {
                    added.Add(moniker);
                }
            }
        }

        return added;
    }
}
