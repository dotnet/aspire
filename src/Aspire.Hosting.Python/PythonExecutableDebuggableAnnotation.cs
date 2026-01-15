// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Python;

/// <summary>
/// Marks a Python resource as debuggable.
/// </summary>
/// <remarks>
/// <para>
/// This annotation indicates that the Python resource supports debugging.
/// When this annotation is present, the resource will be configured with appropriate debug launch configurations.
/// </para>
/// </remarks>
internal sealed class PythonExecutableDebuggableAnnotation : IResourceAnnotation
{
}
