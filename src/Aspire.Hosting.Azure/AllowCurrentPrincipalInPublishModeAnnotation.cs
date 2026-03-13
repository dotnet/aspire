// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Indicates that an Azure provisioning resource may resolve well-known principal parameters
/// from the current user principal during publish mode.
/// </summary>
internal sealed class AllowCurrentPrincipalInPublishModeAnnotation : IResourceAnnotation
{
}
