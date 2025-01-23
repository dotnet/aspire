// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Dapr;

[Obsolete("The Dapr integration has been migrated to the Community Toolkit. Please use the CommunityToolkit.Aspire.Hosting.Dapr integration.", error: false)]
internal static class DaprConstants
{
    public static class BuildingBlocks
    {
        public const string PubSub = "pubsub";

        public const string StateStore = "state";
    }
}
