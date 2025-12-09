// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents a callback context for collecting test result files from a test resource.
/// </summary>
public sealed class TestResultsCallbackContext
{
    private readonly List<TestResultFile> _resultFiles = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="TestResultsCallbackContext"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    public TestResultsCallbackContext(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        ServiceProvider = serviceProvider;
        CancellationToken = cancellationToken;
    }

    /// <summary>
    /// Gets the service provider.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Gets the cancellation token.
    /// </summary>
    public CancellationToken CancellationToken { get; }

    /// <summary>
    /// Gets the collection of test result files that have been added.
    /// </summary>
    public IReadOnlyList<TestResultFile> ResultFiles => _resultFiles;

    /// <summary>
    /// Adds a test result file to the context.
    /// </summary>
    /// <param name="file">The file containing test results.</param>
    /// <param name="format">The format of the test results.</param>
    public void AddResultsFile(FileInfo file, TestResultFormat format)
    {
        ArgumentNullException.ThrowIfNull(file);

        _resultFiles.Add(new TestResultFile(file, format));
    }
}

/// <summary>
/// Represents a test result file with its format.
/// </summary>
/// <param name="File">The file containing test results.</param>
/// <param name="Format">The format of the test results.</param>
public readonly record struct TestResultFile(FileInfo File, TestResultFormat Format);
