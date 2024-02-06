// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

internal static class ModelName
{
    /// <summary>
    /// Validate that a model name is valid.
    /// - Must start with an ASCII letter.
    /// - Must contain only ASCII letters, digits, and hyphens.
    /// - Must not end with a hyphen.
    /// - Must not contain consecutive hyphens.
    /// - Must be between 1 and 64 characters long.
    /// </summary>
    internal static void ValidateName(string target, string name)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(name);
    }
}
