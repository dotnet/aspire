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
    [Fact]
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

    [Fact]
    public void Render_DashboardClientEnabled_HtmlInName_Success()
    {
        // Arrange
        Services.AddSingleton<IConfiguration>(new ConfigurationManager());
        Services.AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance);
        Services.AddSingleton<IDashboardClient, MockDashboardClient>();

        // Act
        var cut = RenderComponent<ApplicationName>();

        // Assert
        cut.MarkupMatches("&lt;marquee&gt;An HTML title!&lt;/marquee&gt;");
    }

    private sealed class MockDashboardClient : IDashboardClient
    {
        public bool IsEnabled => true;
        public Task WhenConnected => Task.CompletedTask;
        public string ApplicationName => "<marquee>An HTML title!</marquee>";
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>>? SubscribeConsoleLogs(string resourceName, CancellationToken cancellationToken) => throw new NotImplementedException();
        public ResourceViewModelSubscription SubscribeResources() => throw new NotImplementedException();
    }
}
