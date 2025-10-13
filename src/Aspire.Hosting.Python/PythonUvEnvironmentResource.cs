// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// A resource that represents a UV environment setup task for a Python application.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The parent Python application resource.</param>
internal sealed class PythonUvEnvironmentResource(string name, PythonAppResource parent)
    : ExecutableResource(name, "uv", parent.WorkingDirectory)
{
}
