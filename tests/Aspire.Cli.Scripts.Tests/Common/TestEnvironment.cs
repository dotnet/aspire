// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Provides an isolated, temporary test environment for CLI script testing.
/// CRITICAL: All operations are confined to temporary directories - no user directory modifications.
/// </summary>
public class TestEnvironment : IDisposable
{
    /// <summary>
    /// Temporary directory for this test environment. Always in system temp location.
    /// </summary>
    public string TempDirectory { get; }

    /// <summary>
    /// Mock home directory within the temporary directory.
    /// Used to set HOME/USERPROFILE environment variables for script testing.
    /// </summary>
    public string MockHome { get; }

    public TestEnvironment()
    {
        // SAFETY: Always use Path.GetTempPath() - never user directories
        TempDirectory = Path.Combine(Path.GetTempPath(), $"aspire-test-{Guid.NewGuid():N}");
        MockHome = Path.Combine(TempDirectory, "home");
        Directory.CreateDirectory(MockHome);
        
        // Create a minimal shell config file to prevent script errors when trying to modify shell profiles
        // This file is in the isolated temp directory and will be cleaned up
        var bashrc = Path.Combine(MockHome, ".bashrc");
        File.WriteAllText(bashrc, "# Mock bashrc for testing\n");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, true);
            }
        }
        catch
        {
            // Best effort cleanup - don't fail tests on cleanup issues
        }
    }
}
