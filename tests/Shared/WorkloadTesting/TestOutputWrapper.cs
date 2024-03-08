// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;
using Xunit.Sdk;
using System.Globalization;

namespace Aspire.Workload.Tests;

public class TestOutputWrapper(ITestOutputHelper? testOutputHelper = null, IMessageSink? messageSink = null) : ITestOutputHelper
{
    public void WriteLine(string message)
    {
        testOutputHelper?.WriteLine(message);
        messageSink?.OnMessage(new DiagnosticMessage(message));

        if (EnvironmentVariables.ShowBuildOutput)
        {
            Console.WriteLine(message);
        }
    }

    public void WriteLine(string format, params object[] args)
    {
        testOutputHelper?.WriteLine(format, args);
        messageSink?.OnMessage(new DiagnosticMessage(string.Format(CultureInfo.CurrentCulture, format, args)));
        if (EnvironmentVariables.ShowBuildOutput)
        {
            Console.WriteLine(format, args);
        }
    }
}
