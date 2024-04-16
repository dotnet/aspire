// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Logging.Testing;

public class TestLoggerFactory : ILoggerFactory
{
    private readonly ITestSink _sink;
    private readonly bool _enabled;

    public TestLoggerFactory(ITestSink sink, bool enabled)
    {
        _sink = sink;
        _enabled = enabled;
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new TestLogger(categoryName, _sink, _enabled);
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
