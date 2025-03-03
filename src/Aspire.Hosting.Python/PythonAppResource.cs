// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// A resource that represents a python executable or app.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="executablePath">The path to the executable used to run the python app.</param>
/// <param name="appDirectory">The path to the directory containing the python app.</param>
public class PythonAppResource(string name, string executablePath, string appDirectory)
    : ExecutableResource(name, executablePath, appDirectory), IResourceWithServiceDiscovery;
