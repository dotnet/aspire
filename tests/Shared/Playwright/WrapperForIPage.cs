// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Playwright;
using Xunit;

// Used to wrap IPage and flag console errors and page errors
public class WrapperForIPage
{
    public IPage Page { get; init; }
    public bool HasErrors { get; private set; }
    private readonly ITestOutputHelper _testOutput;

    public WrapperForIPage(IPage page, ITestOutputHelper testOutput)
    {
        Page = page;
        _testOutput = testOutput;

        page.Console += (_, e) =>
        {
            _testOutput.WriteLine($"[browser-console] {e.Text}");
            HasErrors = e.Text.Contains("Error: WebSocket closed with status code") || e.Text.Contains("net::ERR_NETWORK_CHANGED");
        };
        page.PageError += (_, e) =>
        {
            _testOutput.WriteLine($"[browser-error] {e}");
            HasErrors = true;
        };
    }

    public Task<IResponse?> ReloadAsync(PageReloadOptions? options = null)
    {
        HasErrors = false;
        return Page.ReloadAsync(options);
    }

    public Task<IResponse?> GotoAsync(string url, PageGotoOptions? options = null)
    {
        HasErrors = false;
        return Page.GotoAsync(url, options);
    }
}
