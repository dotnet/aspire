// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Aspire.Hosting;

/// <summary>
/// Represents a collection of resource groups.
/// </summary>
public class ResourceGroupCollection : Collection<IDistributedApplicationGroupBuilder>;
