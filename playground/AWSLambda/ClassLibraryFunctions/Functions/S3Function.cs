// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.Lambda.S3Events;

namespace ClassLibraryFunctions.Functions;

public class S3Function
{
    public string Handle(S3Event @event)
    {
        return "S3 Operation Complete";
    }
}
