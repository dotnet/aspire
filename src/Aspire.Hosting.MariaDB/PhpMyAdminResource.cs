// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.MariaDB;

/// <summary>
/// Resource representing PhpMyAdmin container.
/// </summary>
/// <param name="name">Name of resource.</param>
public sealed class PhpMyAdminContainerResource(string name) : ContainerResource(name)
{
}
