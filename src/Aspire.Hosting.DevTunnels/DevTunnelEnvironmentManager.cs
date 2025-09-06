// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Aspire.Hosting.DevTunnels;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
internal sealed class DevTunnelEnvironmentManager(IDevTunnelClient devTunnelClient, IInteractionService interactionService) : IDisposable
{
    private readonly IDevTunnelClient _devTunnelClient = devTunnelClient;
    private readonly IInteractionService _interactionService = interactionService;
    private readonly object _loginLock = new();
    private Task? _loginTask;
    private CancellationTokenSource? _loginCts;

    // public void EnsureCliInstalledAsync(string? cliPath = "devtunnel")
    // {

    // }

    public Task EnsureUserLoggedInAsync(CancellationToken cancellationToken = default)
    {
        Task? running = null;

        lock (_loginLock)
        {
            if (_loginTask is { IsCompleted: false })
            {
                // Already running
                running = _loginTask;
            }
            else
            {
                // No current running login task, start a new one
                _loginCts?.Dispose();
                _loginCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                running = _loginTask = StartLoginFlowAsync(_loginCts.Token);
            }

            _ = _loginTask.ContinueWith(t =>
            {
                // Clear the task when it completes so a new one can be started if needed
                lock (_loginLock)
                {
                    if (ReferenceEquals(t, _loginTask))
                    {
                        _loginTask = null;
                    }
                }
            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }

        return running.WaitAsync(cancellationToken);
    }

    public void Dispose()
    {
        lock (_loginLock)
        {
            _loginCts?.Cancel();
            _loginCts?.Dispose();
            _loginCts = null;
            _loginTask = null;
        }
    }

    private async Task StartLoginFlowAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            var loginStatus = await _devTunnelClient.GetUserLoginStatusAsync(cancellationToken).ConfigureAwait(false);
            if (loginStatus.IsLoggedIn)
            {
                // Already logged in
                break;
            }
            else
            {
                // Not logged in, prompt the user to login
                var result = await _interactionService.PromptNotificationAsync(
                    "Dev tunnels",
                    $"Dev tunnels requires authentication to continue:",
                    new()
                    {
                        Intent = MessageIntent.Warning,
                        PrimaryButtonText = "Login with Microsoft",
                        SecondaryButtonText = "Login with GitHub",
                        ShowSecondaryButton = true,
                        ShowDismiss = false
                    },
                    cancellationToken).ConfigureAwait(false);

                var selectedProvider = result.Data ? LoginProvider.Microsoft : LoginProvider.GitHub;
                // Check again in case they logged in from another window while we were prompting
                loginStatus = await _devTunnelClient.GetUserLoginStatusAsync(cancellationToken).ConfigureAwait(false);

                if (!loginStatus.IsLoggedIn || loginStatus.Provider != selectedProvider)
                {
                    // Trigger the login flow
                    loginStatus = await _devTunnelClient.UserLoginAsync(selectedProvider, cancellationToken).ConfigureAwait(false);

                    if (loginStatus.IsLoggedIn)
                    {
                        // Successfully logged in
                        break;
                    }
                }
                else
                {
                    // Logged in from another window while we were prompting
                    break;
                }

                // Still not logged in, loop to prompt again
            }
        }
    }
}
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
