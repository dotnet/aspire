// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.Lambda.Core;

namespace ClassLibraryFunctions;

public class EmailFunction
{
    public Task<string> InformCustomer(SendEmail value, ILambdaContext context)
    {
        return Task.FromResult($"Email has been sent to {value.Email}");
    }
}

public class SendEmail
{
    public required string Email { get; set; }
    public required string Subject { get; set; }
    public required string Body { get; set; }
}
