// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace Aspire.Cli.Backchannel;

internal sealed class FailedToConnectBackchannelConnection(string message, Process process, Exception innerException) : Exception(message, innerException)
{
    public Process Process => process;
}