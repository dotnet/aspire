// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// An interface that allows the value to list its references.
/// </summary>
public interface IValueWithReferences
{
    /// <summary>
    /// The referenced objects of the value.
    /// </summary>
    public IEnumerable<object> References { get; }

    /// <summary>
    /// Accepts a visitor to visit the references of the value.
    /// </summary>
    /// <param name="visitor">The visitor to accept.</param>
    /// <returns></returns>
    public async Task AcceptAsync(IResourceVisitor visitor)
    {
        foreach (var reference in References)
        {
            await visitor.VisitAsync(reference).ConfigureAwait(false);
        }
    }
}
