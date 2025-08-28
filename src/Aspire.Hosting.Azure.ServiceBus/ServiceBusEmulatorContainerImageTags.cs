// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Azure.ServiceBus;

internal static class ServiceBusEmulatorContainerImageTags
{
    /// <remarks>mcr.microsoft.com</remarks>
    public const string Registry = "mcr.microsoft.com";

    /// <remarks>azure-messaging/servicebus-emulator</remarks>
    public const string Image = "azure-messaging/servicebus-emulator";

    /// <remarks>1.1.2</remarks>
    public const string Tag = "1.1.2";

    /// <remarks>mcr.microsoft.com</remarks>
    public const string SqlServerRegistry = "mcr.microsoft.com";

    /// <remarks>mssql/server</remarks>
    public const string SqlServerImage = "mssql/server";

    /// <remarks>latest</remarks>
    public const string SqlServerTag = "latest";
}
