// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.AWS.Utils;

internal static class DirectoryCopy
{
    public static void CopyDirectory(string sourcePath, string targetPath, bool @override = false)
    {
        if (Directory.Exists(targetPath) && @override)
        {
            Directory.Delete(targetPath, true);
        }
        Directory.CreateDirectory(targetPath);
        //Now Create all of the directories
        foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (var newPath in Directory.GetFiles(sourcePath, "*.*",SearchOption.AllDirectories))
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}
