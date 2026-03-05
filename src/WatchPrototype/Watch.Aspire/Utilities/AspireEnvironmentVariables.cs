// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.DotNet.Watch;

internal static class AspireEnvironmentVariables
{
    public static TimeSpan PipeConnectionTimeout
        => EnvironmentVariables.ReadTimeSpanSeconds("ASPIRE_WATCH_PIPE_CONNECTION_TIMEOUT_SECONDS") ?? TimeSpan.FromSeconds(30);
}
