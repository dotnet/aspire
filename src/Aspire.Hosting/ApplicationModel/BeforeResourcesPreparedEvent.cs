// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// This event is raised by orchestrators before the resources are prepared.
/// </summary>
public class BeforeResourcesPreparedEvent() : IDistributedApplicationEvent
{
}
