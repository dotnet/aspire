// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Common.Internal;

internal static class ProjectPathUtils
{
    private static readonly string? s_aspireProjectRootEnvVar = Environment.GetEnvironmentVariable("ASPIRE_PROJECT_ROOT");
    public static string? FindMatchingProjectPath(string? originalProjectPath, string label = "")
    {
        if (string.IsNullOrEmpty(s_aspireProjectRootEnvVar) || !Directory.Exists(s_aspireProjectRootEnvVar) || string.IsNullOrEmpty(originalProjectPath) || File.Exists(originalProjectPath))
        {
            Console.WriteLine($"[{label}] root: {s_aspireProjectRootEnvVar}, originalProjectPath: {originalProjectPath}");
            return originalProjectPath;
        }

        // Console.WriteLine($"s_aspireProjectRootEnvVar: {root}");
        Console.WriteLine($">> [{label}] originalProjectPath: {originalProjectPath}");

        string filename = Path.GetFileName(originalProjectPath);

        string relativeTargetPath = Path.GetDirectoryName(originalProjectPath)!;
        Console.WriteLine($"%% [{label}] starting: relativeTargetPath: {relativeTargetPath}");

        string relativeParentPath = "";
        while (true)
        {
            string parentName = Path.GetFileName(relativeTargetPath);
            if (string.IsNullOrEmpty(parentName))
            {
                Console.WriteLine($"%% [{label}] No parent found for {relativeTargetPath} for {originalProjectPath}");
                break;
            }
            // prepend the parent name to the relativeParentPath
            relativeParentPath = relativeParentPath.Length == 0 ? parentName : Path.Combine(parentName, relativeParentPath);

            // FIXME: check if this should this be done at the end of the block?
            relativeTargetPath = Path.GetDirectoryName(relativeTargetPath)!;
            Console.WriteLine($"\t%% [{label}] relativePathToTry: {relativeParentPath}");
            if (relativeParentPath == null)
            {
                break;
            }

            string projectPathToTry = Path.Combine(s_aspireProjectRootEnvVar, relativeParentPath, filename);
            Console.WriteLine($"\t%% [{label}] projectPathToTry: {projectPathToTry}");

            if (File.Exists(projectPathToTry))
            {
                Console.WriteLine($"\t\t%% [{label}] Using root: {s_aspireProjectRootEnvVar} => returning {projectPathToTry}");
                return projectPathToTry;
            }
        }

        return originalProjectPath;
    }
}
