// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.SignalR;
internal static class SignalREmulatorContainerImageTags
{
    /// <remarks>mcr.microsoft.com</remarks>
    public const string Registry = "mcr.microsoft.com";

    /// <remarks>signalr/signalr-emulator</remarks>
    public const string Image = "signalr/signalr-emulator";

    /// <remarks>latest</remarks>
    public const string Tag = "latest";
}
