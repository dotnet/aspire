// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Cli.Scripts.Tests.Common;

/// <summary>
/// Provides an isolated temporary directory for test execution.
/// CRITICAL: All operations must use temporary directories to ensure no user environment modifications.
/// </summary>
public sealed class TestEnvironment : IDisposable
{
    /// <summary>
    /// Gets the root temporary directory for this test environment.
    /// </summary>
    public string TempDirectory { get; }

    /// <summary>
    /// Gets a mock home directory within the test environment.
    /// Can be used as HOME or USERPROFILE in environment variables for scripts.
    /// </summary>
    public string MockHome { get; }

    /// <summary>
    /// Creates a new isolated test environment with a unique temporary directory.
    /// </summary>
    public TestEnvironment()
    {
        // CRITICAL: Must use Path.GetTempPath() - never user directories
        TempDirectory = Path.Combine(Path.GetTempPath(), $"aspire-test-{Guid.NewGuid():N}");
        MockHome = Path.Combine(TempDirectory, "home");
        
        Directory.CreateDirectory(TempDirectory);
        Directory.CreateDirectory(MockHome);
    }

    /// <summary>
    /// Cleans up the test environment by deleting the temporary directory.
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
            // Best effort cleanup - don't fail test if cleanup fails
        }
    }
}
