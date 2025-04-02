// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.Templates.Tests;

/// <summary>
/// This fixture creates a new project using the `aspire-starter` template, and runs that
/// </summary>
public sealed class StarterTemplateFixture : TemplateAppFixture
{
    public StarterTemplateFixture(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink, "aspire-starter")
    {
    }
}
