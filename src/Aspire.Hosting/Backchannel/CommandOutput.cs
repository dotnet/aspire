// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.Backchannel;

internal class CommandOutput
{
    public required string Text { get; set; }
    public bool IsError { get; set; }
}
