// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Templates.Tests;

public class ToolCommandException : Exception
{
    public CommandResult? Result { get; }
    public ToolCommandException(string message, CommandResult? result) : base(message)
    {
        Result = result;
    }
}
