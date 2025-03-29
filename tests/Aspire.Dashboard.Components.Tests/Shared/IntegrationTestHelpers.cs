// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Xunit;

namespace Aspire.Dashboard.Components.Tests.Shared;

public static class IntegrationTestHelpers
{
    public static ILoggerFactory CreateLoggerFactory(ITestOutputHelper testOutputHelper, ITestSink? testSink = null)
    {
        return LoggerFactory.Create(builder =>
        {
            builder.AddXunit(testOutputHelper, LogLevel.Trace, DateTimeOffset.UtcNow);
            builder.SetMinimumLevel(LogLevel.Trace);
            if (testSink != null)
            {
                builder.AddProvider(new TestLoggerProvider(testSink));
            }
        });
    }
}
