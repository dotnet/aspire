// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.Templates.Tests;

/// <summary>
/// This fixture runs a aspire-starter template created with --use-redis-cache
/// </summary>
public sealed class StarterTemplateWithRedisCacheFixture : TemplateAppFixture
{
    public StarterTemplateWithRedisCacheFixture(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink, "aspire-starter", "--use-redis-cache")
    {
    }
}
