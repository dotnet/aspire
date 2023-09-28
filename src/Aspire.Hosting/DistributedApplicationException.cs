// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting;

public class DistributedApplicationException : Exception
{
    public DistributedApplicationException() { }
    public DistributedApplicationException(string message) : base(message) { }
    public DistributedApplicationException(string message, Exception inner) : base(message, inner) { }
}
