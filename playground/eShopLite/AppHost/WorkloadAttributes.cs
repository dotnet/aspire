// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

// In our released product this attribute is added automatically by the SDK, here we reference
// the path relative to the AppHost working directory and code inside the ApplicationExecutor
// resolves it to the full path.
[assembly:AssemblyMetadata("aspiredashboardpath", "..\\..\\..\\artifacts\\bin\\Aspire.Dashboard\\Debug\\net8.0\\Aspire.Dashboard.dll")]
