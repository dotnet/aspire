// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit.Sdk;

namespace Aspire.Templates.Tests;

/// <summary>
/// This fixture ensures the TestProject.AppHost application is started before a test is executed.
///
/// Represents the the IntegrationServiceA project in the test application used to send HTTP requests
/// to the project's endpoints.
/// </summary>
public sealed class EmptyTemplateRunFixture : TemplateAppFixture
{
    public EmptyTemplateRunFixture(IMessageSink diagnosticMessageSink)
        : base(diagnosticMessageSink, "aspire")
    {
    }
}
