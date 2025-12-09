// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// Represents an annotation that provides a callback to collect test result information from a test resource.
/// </summary>
public sealed class TestResultsCallbackAnnotation : IResourceAnnotation
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestResultsCallbackAnnotation"/> class.
    /// </summary>
    /// <param name="callback">The callback to invoke to collect test results.</param>
    public TestResultsCallbackAnnotation(Func<TestResultsCallbackContext, Task> callback)
    {
        ArgumentNullException.ThrowIfNull(callback);

        Callback = callback;
    }

    /// <summary>
    /// Gets the callback to invoke to collect test results.
    /// </summary>
    public Func<TestResultsCallbackContext, Task> Callback { get; }
}
