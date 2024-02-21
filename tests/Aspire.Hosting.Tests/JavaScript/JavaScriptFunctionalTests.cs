// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Tests.Helpers;
using Xunit;

namespace Aspire.Hosting.Tests.JavaScript;

[Collection("JavaScriptApp")]
public class JavaScriptFunctionalTests
{
    private readonly JavaScriptAppFixture _javaScriptRuntimeFixture;

    public JavaScriptFunctionalTests(JavaScriptAppFixture nodeJsFixture)
    {
        _javaScriptRuntimeFixture = nodeJsFixture;
    }
    // can this be a reassignable variable? eg: varXYZ = "node"
    [LocalOnlyFact(varXYZ)]
    public async Task VerifyJavaScriptAppWorks()
    {
        var testProgram = _javaScriptRuntimeFixture.TestProgram;
        var client = _javaScriptRuntimeFixture.HttpClient;

        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));

        var response0 = await testProgram.JavaScriptAppBuilder!.HttpGetStringWithRetryAsync(client, "http", "/", cts.Token);
        var response1 = await testProgram.JavaScriptCLIAppBuilder!.HttpGetStringWithRetryAsync(client, "http", "/", cts.Token);

        Assert.Equal("Hello from JavaScript Runtime!", response0);
        Assert.Equal("Hello from JavaScript Runtime!", response1);
    }
}
