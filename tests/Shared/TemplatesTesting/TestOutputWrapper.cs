// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;
using System.Globalization;
using Xunit;

namespace Aspire.Templates.Tests;

public class TestOutputWrapper(ITestOutputHelper? testOutputHelper = null, IMessageSink? messageSink = null, bool forceShowBuildOutput = false) : ITestOutputHelper
{
    public string Output => testOutputHelper?.Output ?? string.Empty;

    public void Write(string message)
    {
        testOutputHelper?.Write(message);
        messageSink?.OnMessage(new DiagnosticMessage(message));

        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.Write(message);
        }
    }

    public void Write(string format, params object[] args)
    {
        testOutputHelper?.Write(format, args);
        messageSink?.OnMessage(new DiagnosticMessage(string.Format(CultureInfo.CurrentCulture, format, args)));
        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.Write(format, args);
        }
    }

    public void WriteLine(string message)
    {
        testOutputHelper?.WriteLine(message);
        messageSink?.OnMessage(new DiagnosticMessage(message));

        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.WriteLine(message);
        }
    }

    public void WriteLine(string format, params object[] args)
    {
        testOutputHelper?.WriteLine(format, args);
        messageSink?.OnMessage(new DiagnosticMessage(string.Format(CultureInfo.CurrentCulture, format, args)));
        if (forceShowBuildOutput || EnvironmentVariables.ShowBuildOutput)
        {
            Console.WriteLine(format, args);
        }
    }
}
