// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.EventHubs;

internal static class EventHubsEmulatorContainerImageTags
{
    /// <summary>mcr.microsoft.com</summary>
    public const string Registry = "mcr.microsoft.com";

    /// <summary>azure-messaging/eventhubs-emulator</summary>
    public const string Image = "azure-messaging/eventhubs-emulator";

    /// <summary>latest</summary>
    public const string Tag = "latest"; // latest is the only arch-agnostic tag
}
