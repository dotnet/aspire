// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Python;

/// <summary>
/// Specifies the type of entrypoint for a Python application.
/// </summary>
public enum EntrypointType
{
    /// <summary>
    /// A standalone executable from the virtual environment (e.g., "uvicorn", "flask", "pytest").
    /// </summary>
    Executable,

    /// <summary>
    /// A Python script file to execute directly (e.g., "main.py", "app.py").
    /// </summary>
    Script,

    /// <summary>
    /// A Python module to run via <c>python -m &lt;module&gt;</c> (e.g., "flask", "uvicorn").
    /// </summary>
    Module
}
