// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Abstractions;

namespace Aspire.Workload.Tests;

public sealed class StarterTemplateFixture_PreviousTFM : TemplateAppFixture
{
    public StarterTemplateFixture_PreviousTFM(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink, "aspire-starter", tfm: TestTargetFramework.Previous)
    {
    }
}
