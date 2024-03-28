// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

internal class FailedToApplyEnvironmentException : DistributedApplicationException
{
    public FailedToApplyEnvironmentException() { }
    public FailedToApplyEnvironmentException(string message) : base(message) { }
    public FailedToApplyEnvironmentException(string message, Exception inner) : base(message, inner) { }
}
