// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// A resource that represents a Python project.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="executablePath">The path to the executable used to run the python project.</param>
/// <param name="projectDirectory">The path to the directory containing the python project.</param>
public class PythonProjectResource(string name, string executablePath, string projectDirectory)
    : ExecutableResource(name, executablePath, projectDirectory)
{

}
