// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.SignalR;
internal static class SignalREmulatorContainerImageTags
{
    /// <summary>mcr.microsoft.com</summary>
    public const string Registry = "mcr.microsoft.com";

    /// <summary>signalr/signalr-emulator</summary>
    public const string Image = "signalr/signalr-emulator";

    /// <summary>latest</summary>
    public const string Tag = "latest"; // latest is the only arch-agnostic tag
}
