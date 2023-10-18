// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;

namespace Aspire.Hosting.Azure;

public class AzureKeyVaultResource(string name) : DistributedApplicationResource(name), IAzureResource, IDistributedApplicationResourceWithConnectionString
{
    public Uri? VaultUri { get; set; }

    public string? GetConnectionString() => VaultUri?.ToString();
}
