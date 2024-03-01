// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Dashboard.Model;
using Bunit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace Aspire.Dashboard.Components.Tests.Controls;

public class ApplicationNameTests : TestContext
{
    [Fact(Skip = "Bunit depends on IJSUnmarshalledRuntime which has been removed. See issue https://github.com/dotnet/aspire/issues/2576")]
    public void Render_DashboardClientDisabled_Success()
    {
        // Arrange
        Services.AddSingleton<IConfiguration>(new ConfigurationManager());
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton<IDashboardClient, DashboardClient>();

        // Act
        var cut = RenderComponent<ApplicationName>();

        // Assert
        cut.MarkupMatches("Aspire");
    }
}
