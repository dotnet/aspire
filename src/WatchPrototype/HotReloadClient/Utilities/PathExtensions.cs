// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;

namespace System.IO;

internal static partial class PathExtensions
{
#if NET // binary compatibility
    public static bool IsPathFullyQualified(string path)
        => Path.IsPathFullyQualified(path);

    public static string Join(string? path1, string? path2)
        => Path.Join(path1, path2);
#else
    extension(Path)
    {
        public static bool IsPathFullyQualified(string path)
           => Path.DirectorySeparatorChar == '\\'
            ? !IsPartiallyQualified(path.AsSpan())
            : Path.IsPathRooted(path);
    }

    // Copied from https://github.com/dotnet/runtime/blob/a6c5ba30aab998555e36aec7c04311935e1797ab/src/libraries/Common/src/System/IO/PathInternal.Windows.cs#L250

    /// <summary>
    /// Returns true if the path specified is relative to the current drive or working directory.
    /// Returns false if the path is fixed to a specific drive or UNC path.  This method does no
    /// validation of the path (URIs will be returned as relative as a result).
    /// </summary>
    /// <remarks>
    /// Handles paths that use the alternate directory separator.  It is a frequent mistake to
    /// assume that rooted paths (Path.IsPathRooted) are not relative.  This isn't the case.
    /// "C:a" is drive relative- meaning that it will be resolved against the current directory
    /// for C: (rooted, but relative). "C:\a" is rooted and not relative (the current directory
    /// will not be used to modify the path).
    /// </remarks>
    private static bool IsPartiallyQualified(ReadOnlySpan<char> path)
    {
        if (path.Length < 2)
        {
            // It isn't fixed, it must be relative.  There is no way to specify a fixed
            // path with one character (or less).
            return true;
        }

        if (IsDirectorySeparator(path[0]))
        {
            // There is no valid way to specify a relative path with two initial slashes or
            // \? as ? isn't valid for drive relative paths and \??\ is equivalent to \\?\
            return !(path[1] == '?' || IsDirectorySeparator(path[1]));
        }

        // The only way to specify a fixed path that doesn't begin with two slashes
        // is the drive, colon, slash format- i.e. C:\
        return !((path.Length >= 3)
            && (path[1] == Path.VolumeSeparatorChar)
            && IsDirectorySeparator(path[2])
            // To match old behavior we'll check the drive character for validity as the path is technically
            // not qualified if you don't have a valid drive. "=:\" is the "=" file's default data stream.
            && IsValidDriveChar(path[0]));
    }

    /// <summary>
    /// True if the given character is a directory separator.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool IsDirectorySeparator(char c)
    {
        return c == Path.DirectorySeparatorChar || c == Path.AltDirectorySeparatorChar;
    }

    /// <summary>
    /// Returns true if the given character is a valid drive letter
    /// </summary>
    internal static bool IsValidDriveChar(char value)
    {
        return (uint)((value | 0x20) - 'a') <= (uint)('z' - 'a');
    }

    // Copied from https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/IO/Path.cs

    private static readonly string s_directorySeparatorCharAsString = Path.DirectorySeparatorChar.ToString();

    extension(Path)
    {
        public static string Join(string? path1, string? path2)
        {
            if (string.IsNullOrEmpty(path1))
                return path2 ?? string.Empty;

            if (string.IsNullOrEmpty(path2))
                return path1;

            return JoinInternal(path1, path2);
        }
    }

    private static string JoinInternal(string first, string second)
    {
        Debug.Assert(first.Length > 0 && second.Length > 0, "should have dealt with empty paths");

        bool hasSeparator = IsDirectorySeparator(first[^1]) || IsDirectorySeparator(second[0]);

        return hasSeparator ?
            string.Concat(first, second) :
            string.Concat(first, s_directorySeparatorCharAsString, second);
    }
#endif
}
