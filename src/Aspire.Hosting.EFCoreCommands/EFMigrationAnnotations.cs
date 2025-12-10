// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting;

/// <summary>
/// Annotation to indicate that database migrations should be run when the AppHost starts.
/// </summary>
internal sealed class RunDatabaseUpdateOnStartAnnotation : IResourceAnnotation
{
}

/// <summary>
/// Annotation to indicate that a migration script should be generated during publishing.
/// </summary>
internal sealed class PublishAsMigrationScriptAnnotation : IResourceAnnotation
{
}

/// <summary>
/// Annotation to indicate that a migration bundle should be generated during publishing.
/// </summary>
internal sealed class PublishAsMigrationBundleAnnotation : IResourceAnnotation
{
}
