// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace SamplesIntegrationTests.Infrastructure;

/// <summary>
/// An <see cref="ILoggerProvider"/> that creates <see cref="ILogger"/> instances that output to the supplied <see cref="ITestOutputHelper"/>.
/// </summary>
internal sealed class XUnitLoggerProvider(ITestOutputHelper testOutputHelper) : ILoggerProvider
{
    private readonly LoggerExternalScopeProvider _scopeProvider = new();

    public ILogger CreateLogger(string categoryName)
    {
        return new XUnitLogger(testOutputHelper, _scopeProvider, categoryName);
    }

    public void Dispose()
    {
    }
}
