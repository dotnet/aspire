// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Cli.Commands;
using Aspire.Cli.Tests.Utils;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Aspire.Cli.Tests.Commands;

public class NewCommandTests
{
    [Fact]
    public async Task NewCommandWithHelpArgumentReturnsZero()
    {
        var services = CliTestHelper.CreateServiceCollection();
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<RootCommand>();
        var result = command.Parse("new --help");

        var exitCode = await result.InvokeAsync().WaitAsync(CliTestConstants.DefaultTimeout);
        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task NewCommandPromptsForProjectNameWhenNotSuppliedOnCommandLine()
    {
        var prompted = new TaskCompletionSource<string>();

        var options = new FakeInteractionServiceOptions()
        {
            PromptForStringAsyncCallback = (promptText, defaultValue, validator, cancellationToken) => {
                prompted.SetResult(promptText);
                throw new InvalidOperationException();
            }
        };

        var fakeInteractionService = new FakeInteractionService(options);

        var services = CliTestHelper.CreateServiceCollection(options => {
            options.InteractiveServiceFactory = _ => fakeInteractionService;
        });
        var provider = services.BuildServiceProvider();

        var command = provider.GetRequiredService<NewCommand>();
        var result = command.Parse("new");

        var cts = new CancellationTokenSource();
        var pendingNewCommand = result.InvokeAsync(cts.Token);

        cts.Cancel();
        var prompt = await prompted.Task;

        Assert.Equal("blah", prompt);
    }
}
