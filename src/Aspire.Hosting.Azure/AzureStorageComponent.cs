// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureStorageComponent : IAzureComponent
{
    public ComponentMetadataCollection Annotations { get; } = new();

    public string? ConnectionString { get; set; }
    public string? AccountName { get; set; }
}
