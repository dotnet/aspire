// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Annotation to indicate that a project should have Playwright testing enabled in Azure App Service.
/// </summary>
internal sealed class EnablePlaywrightTestingAnnotation : IResourceAnnotation
{
}
