// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Scripts.Tests;

/// <summary>
/// Provides safe, isolated temporary directory environment for CLI script tests.
/// CRITICAL: All operations must be confined to temp directories to ensure zero risk to user environments.
/// </summary>
public sealed class TestEnvironment : IDisposable
{
    /// <summary>
    /// Root temporary directory for this test instance.
    /// </summary>
    public string TempDirectory { get; }

    /// <summary>
    /// Mock HOME directory within the temp directory.
    /// Can be used to override HOME/USERPROFILE environment variables safely.
    /// </summary>
    public string MockHome { get; }

    /// <summary>
    /// Creates a new isolated test environment with temporary directories.
    /// </summary>
    public TestEnvironment()
    {
        // CRITICAL: Must use Path.GetTempPath() - never user directories
        TempDirectory = Path.Combine(Path.GetTempPath(), $"aspire-test-{Guid.NewGuid():N}");
        MockHome = Path.Combine(TempDirectory, "home");
        
        // Create the directories
        Directory.CreateDirectory(TempDirectory);
        Directory.CreateDirectory(MockHome);
    }

    /// <summary>
    /// Cleans up the temporary directory.
    /// </summary>
    public void Dispose()
    {
        try
        {
            if (Directory.Exists(TempDirectory))
            {
                Directory.Delete(TempDirectory, recursive: true);
            }
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }
}
