// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// Represents a UV environment setup task that runs before a Python application starts.
/// </summary>
/// <remarks>
/// <para>
/// This resource executes the <c>uv sync</c> command to ensure that a Python virtual environment
/// is created and all dependencies specified in the project's pyproject.toml file are installed
/// and synchronized. The parent Python application waits for this resource to complete successfully
/// before starting.
/// </para>
/// <para>
/// UV (https://github.com/astral-sh/uv) is a fast Python package and project manager written in Rust.
/// It can replace pip, pip-tools, pipx, poetry, pyenv, virtualenv, and more, providing significantly
/// faster dependency resolution and installation.
/// </para>
/// <para>
/// This is an internal resource automatically created by the <see cref="PythonAppResourceBuilderExtensions.WithUvEnvironment{T}"/>
/// extension method. It is configured as a child resource that must complete before the Python application runs.
/// </para>
/// </remarks>
/// <param name="name">
/// The name of the UV environment resource. Typically follows the pattern "{parentAppName}-uv-environment".
/// </param>
/// <param name="parent">
/// The parent Python application resource that this UV environment setup task supports.
/// The UV command runs in the parent application's working directory.
/// </param>
internal sealed class PythonUvEnvironmentResource(string name, PythonAppResource parent)
    : ExecutableResource(name, "uv", parent.WorkingDirectory)
{
}
