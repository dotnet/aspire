// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.ApplicationModel;

public class AzureKeyVaultResource(string name) : Resource(name), IAzureResource, IResourceWithConnectionString
{
    public Uri? VaultUri { get; set; }

    public string? GetConnectionString() => VaultUri?.ToString();
}
