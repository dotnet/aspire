// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An interface that allows the value to be provided for an environment variable.
/// </summary>
public interface IValueProvider
{
    /// <summary>
    /// Gets the value for use as an environment variable.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/>.</param>
    /// <returns></returns>
    public ValueTask<string?> GetValueAsync(CancellationToken cancellationToken = default);
}
