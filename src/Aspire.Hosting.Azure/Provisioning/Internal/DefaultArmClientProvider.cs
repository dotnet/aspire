// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Core;
using Azure.ResourceManager;

namespace Aspire.Hosting.Azure.Provisioning.Internal;

/// <summary>
/// Default implementation of <see cref="IArmClientProvider"/>.
/// </summary>
internal sealed class DefaultArmClientProvider : IArmClientProvider
{
    public IArmClient GetArmClient(TokenCredential credential, string subscriptionId)
    {
        var armClient = new ArmClient(credential, subscriptionId);
        return new DefaultArmClient(armClient);
    }
}