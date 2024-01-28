// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace Aspire.Hosting.Utils;

public static class FileNameSuffixes
{
    public const string DepsJson = ".deps.json";
    public const string RuntimeConfigJson = ".runtimeconfig.json";
    public const string RuntimeConfigDevJson = ".runtimeconfig.dev.json";

    public static PlatformFileNameSuffixes CurrentPlatform
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return OSX;
            }
            else
            {
                // assume everything else is Unix to avoid modifying this file
                // every time a new platform is introduced in runtime.
                return Unix;
            }
        }
    }

    public static PlatformFileNameSuffixes DotNet { get; } = new PlatformFileNameSuffixes
    {
        DynamicLib = ".dll",
        Exe = ".exe",
        ProgramDatabase = ".pdb",
        StaticLib = ".lib"
    };

    public static PlatformFileNameSuffixes Windows { get; } = new PlatformFileNameSuffixes
    {
        DynamicLib = ".dll",
        Exe = ".exe",
        ProgramDatabase = ".pdb",
        StaticLib = ".lib"
    };

    public static PlatformFileNameSuffixes OSX { get; } = new PlatformFileNameSuffixes
    {
        DynamicLib = ".dylib",
        Exe = string.Empty,
        ProgramDatabase = ".pdb",
        StaticLib = ".a"
    };

    public static PlatformFileNameSuffixes Unix { get; } = new PlatformFileNameSuffixes
    {
        DynamicLib = ".so",
        Exe = string.Empty,
        ProgramDatabase = ".pdb",
        StaticLib = ".a"
    };

    public struct PlatformFileNameSuffixes
    {
        public string DynamicLib { get; internal set; }

        public string Exe { get; internal set; }

        public string ProgramDatabase { get; internal set; }

        public string StaticLib { get; internal set; }
    }
}
