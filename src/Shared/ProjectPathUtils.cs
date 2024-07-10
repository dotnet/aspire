// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Common.Internal;

internal static class ProjectPathUtils
{
    public static string? FindMatchingProjectPath(string? root, string? originalProjectPath, string label = "")
    {
        if (string.IsNullOrEmpty(root) || !Directory.Exists(root) || string.IsNullOrEmpty(originalProjectPath) || File.Exists(originalProjectPath))
        {
            Console.WriteLine($"[{label}] root: {root}, originalProjectPath: {originalProjectPath}");
            return originalProjectPath;
        }

        // Console.WriteLine($"root: {root}");
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

            relativeTargetPath = Path.GetDirectoryName(relativeTargetPath)!;
            Console.WriteLine($"\t%% [{label}] relativePathToTry: {relativeParentPath}");
            if (relativeParentPath == null)
            {
                break;
            }

            string projectPathToTry = Path.Combine(root, relativeParentPath, filename);
            Console.WriteLine($"\t%% [{label}] projectPathToTry: {projectPathToTry}");

            if (File.Exists(projectPathToTry))
            {
                Console.WriteLine($"\t\t%% [{label}] Using root: {root} => returning {projectPathToTry}");
                break;
            }
        }

        return originalProjectPath;
    }
}
