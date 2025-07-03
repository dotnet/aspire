// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using Aspire.Cli.Resources;
using Aspire.Cli.Utils;
using Aspire.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using StreamJsonRpc;

namespace Aspire.Cli.Backchannel;

internal interface IExtensionBackchannel
{
    Task ConnectAsync(CancellationToken cancellationToken);
    Task<long> PingAsync(long timestamp, CancellationToken cancellationToken);
    Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken);
    Task DisplaySuccessAsync(string message, CancellationToken cancellationToken);
    Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken);
    Task DisplayErrorAsync(string error, CancellationToken cancellationToken);
    Task DisplayEmptyLineAsync(CancellationToken cancellationToken);
    Task DisplayIncompatibleVersionErrorAsync(string requiredCapability, string appHostHostingSdkVersion, CancellationToken cancellationToken);
    Task DisplayCancellationMessageAsync(CancellationToken cancellationToken);
    Task DisplayLinesAsync(IEnumerable<DisplayLineState> lines, CancellationToken cancellationToken);
    Task DisplayDashboardUrlsAsync((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls, CancellationToken cancellationToken);
    Task ShowStatusAsync(string? status, CancellationToken cancellationToken);
    Task<T?> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter, CancellationToken cancellationToken) where T : notnull;
    Task<bool?> ConfirmAsync(string promptText, bool defaultValue, CancellationToken cancellationToken);
    Task<string?> PromptForStringAsync(string promptText, string? defaultValue, Func<string, ValidationResult>? validator, bool required, CancellationToken cancellationToken);
    Task OpenInIdeAsync(string path, CancellationToken cancellationToken);
    Task LogMessageAsync(LogLevel logLevel, string message, CancellationToken cancellationToken);
    Task DisplayPlainTextAsync(string text, CancellationToken cancellationToken);
}

internal sealed class ExtensionBackchannel(ILogger<ExtensionBackchannel> logger, ExtensionRpcTarget target, IConfiguration configuration) : IExtensionBackchannel
{
    private const string Name = "Aspire Extension";
    private const string BaselineCapability = "baseline.v1";

    private readonly ActivitySource _activitySource = new(nameof(ExtensionBackchannel));
    private readonly TaskCompletionSource<JsonRpc> _rpcTaskCompletionSource = new();
    private readonly string _token = configuration[KnownConfigNames.ExtensionToken]
        ?? throw new InvalidOperationException(ErrorStrings.ExtensionTokenMustBeSet);

    private TaskCompletionSource? _connectionSetupTcs;

    public async Task<long> PingAsync(long timestamp, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent ping with timestamp {Timestamp}", timestamp);

        var responseTimestamp = await rpc.InvokeWithCancellationAsync<long>(
            "PingAsync",
            [_token],
            cancellationToken);

        return responseTimestamp;
    }

    public async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (_connectionSetupTcs is not null)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var cancellationTask = Task.Delay(Timeout.Infinite, linkedCts.Token);
            await Task.WhenAny(_connectionSetupTcs.Task, cancellationTask).ConfigureAwait(false);
            return;
        }

        _connectionSetupTcs = new TaskCompletionSource();

        var endpoint = configuration[KnownConfigNames.ExtensionEndpoint];
        Debug.Assert(endpoint is not null);

        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(50));
        var connectionAttempts = 0;
        logger.LogDebug("Starting backchannel connection to Aspire extension at {Endpoint}", endpoint);

        var startTime = DateTimeOffset.UtcNow;

        do
        {
            connectionAttempts++;

            try
            {
                await ConnectCoreAsync().ConfigureAwait(false);
                logger.LogDebug("Connected to ExtensionBackchannel at {Endpoint}", endpoint);
                _connectionSetupTcs.SetResult();
                return;
            }
            catch (SocketException ex)
            {
                var waitingFor = DateTimeOffset.UtcNow - startTime;
                if (waitingFor > TimeSpan.FromSeconds(10))
                {
                    logger.LogDebug("Slow polling for backchannel connection (attempt {ConnectionAttempts}), {SocketException}", connectionAttempts, ex);
                    await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    // We don't want to spam the logs with our early connection attempts.
                }
            }
            catch (ExtensionIncompatibleException ex)
            {
                logger.LogError(
                    "The Aspire extension is incompatible with the CLI and must be updated to a version that supports the {RequiredCapability} capability.",
                    ex.RequiredCapability
                    );

                // If the extension is incompatible then there is no point
                // trying to reconnect, we should propogate the exception
                // up to the code that needs to back channel so it can display
                // and error message to the user.
                _connectionSetupTcs.SetException(ex);

                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An unexpected error occurred while trying to connect to the backchannel.");
                _connectionSetupTcs.SetException(ex);
                throw;
            }
        } while (await timer.WaitForNextTickAsync(cancellationToken));

        return;

        async Task ConnectCoreAsync()
        {
            try
            {
                using var activity = _activitySource.StartActivity();

                if (_rpcTaskCompletionSource.Task.IsCompleted)
                {
                    throw new InvalidOperationException($"Already connected to {Name} backchannel.");
                }

                logger.LogDebug("Connecting to {Name} backchannel at {SocketPath}", Name, endpoint);
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                var addressParts = endpoint.Split(':');
                if (addressParts.Length != 2 || !int.TryParse(addressParts[1], out var port) || port <= 0 ||
                    port > 65535)
                {
                    throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, ErrorStrings.InvalidSocketPath, endpoint));
                }

                await socket.ConnectAsync(addressParts[0], port, cancellationToken);
                logger.LogDebug("Connected to {Name} backchannel at {SocketPath}", Name, endpoint);

                var stream = new SslStream(new NetworkStream(socket, true),
                    leaveInnerStreamOpen: true,
                    userCertificateValidationCallback: (_, c, _, e) =>
                    {
                        // Server certificate is already considered valid.
                        if (e == SslPolicyErrors.None)
                        {
                            return true;
                        }

                        if (c == null)
                        {
                            return false;
                        }

                        // Certificate isn't immediately valid. Check if it is the same as the one we expect.
                        // It's ok that comparison isn't time constant because this is public information.
                        return GetCertificate().RawData.SequenceEqual(c.GetRawCertData());
                    });

                await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions
                {
                    ClientCertificates = [GetCertificate()],
                }, cancellationToken);

                [UnconditionalSuppressMessage("AotAnalysis", "IL3050:RequiresDynamicCode",
                    Justification = "AddLocalRpcTarget closes on generic types if there are events on the target, which is explicitly disabled.")]
                static void AddLocalRpcTarget(JsonRpc rpc, ExtensionRpcTarget target)
                {
                    // We don't want to notify the client of events because we are not using the
                    // event system in the extension.
                    rpc.AddLocalRpcTarget(target, new JsonRpcTargetOptions() { NotifyClientOfEvents = false });
                }

                var rpc = new JsonRpc(new HeaderDelimitedMessageHandler(stream, stream, BackchannelJsonSerializerContext.CreateRpcMessageFormatter()));
                AddLocalRpcTarget(rpc, target);
                rpc.StartListening();

                var capabilities = await rpc.InvokeWithCancellationAsync<string[]>(
                    "getCapabilities",
                    [_token],
                    cancellationToken);

                if (!capabilities.Any(s => s == BaselineCapability))
                {
                    throw new ExtensionIncompatibleException(
                        string.Format(CultureInfo.CurrentCulture, ErrorStrings.ExtensionIncompatibleWithCli,
                            BaselineCapability),
                        BaselineCapability
                    );
                }

                _rpcTaskCompletionSource.SetResult(rpc);
            }
            catch (RemoteMethodNotFoundException ex)
            {
                logger.LogError(ex,
                    "Failed to connect to {Name} backchannel. The connection must be updated to a version that supports the {BaselineCapability} capability.",
                    Name,
                    BaselineCapability);

                throw new ExtensionIncompatibleException(
                    string.Format(CultureInfo.CurrentCulture, ErrorStrings.ExtensionIncompatibleWithCli,
                        BaselineCapability),
                    BaselineCapability
                );
            }
        }
    }

    public async Task DisplayMessageAsync(string emoji, string message, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displayMessage",
            [_token, emoji, message],
            cancellationToken);
    }

    public async Task DisplaySuccessAsync(string message, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent success message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displaySuccess",
            [_token, message],
            cancellationToken);
    }

    public async Task DisplaySubtleMessageAsync(string message, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent subtle message {Message}", message);

        await rpc.InvokeWithCancellationAsync(
            "displaySubtleMessage",
            [_token, message],
            cancellationToken);
    }

    public async Task DisplayErrorAsync(string error, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent error message {Error}", error);

        await rpc.InvokeWithCancellationAsync(
            "displayError",
            [_token, error],
            cancellationToken);
    }

    public async Task DisplayEmptyLineAsync(CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent empty line");

        await rpc.InvokeWithCancellationAsync(
            "displayEmptyLine",
            [_token],
            cancellationToken);
    }

    public async Task DisplayIncompatibleVersionErrorAsync(string requiredCapability, string appHostHostingSdkVersion, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent incompatible version error for capability {RequiredCapability} with hosting SDK version {AppHostHostingSdkVersion}",
            requiredCapability, appHostHostingSdkVersion);

        await rpc.InvokeWithCancellationAsync(
            "displayIncompatibleVersionError",
            [_token, requiredCapability, appHostHostingSdkVersion],
            cancellationToken);
    }

    public async Task DisplayCancellationMessageAsync(CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent cancellation message");

        await rpc.InvokeWithCancellationAsync(
            "displayCancellationMessage",
            [_token],
            cancellationToken);
    }

    public async Task DisplayLinesAsync(IEnumerable<DisplayLineState> lines, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent lines for display");

        await rpc.InvokeWithCancellationAsync(
            "displayLines",
            [_token, lines],
            cancellationToken);
    }

    public async Task DisplayDashboardUrlsAsync((string BaseUrlWithLoginToken, string? CodespacesUrlWithLoginToken) dashboardUrls, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent dashboard URLs for display");

        var dashboardUrlsState = new DashboardUrlsState()
        {
            BaseUrlWithLoginToken = dashboardUrls.BaseUrlWithLoginToken,
            CodespacesUrlWithLoginToken = dashboardUrls.CodespacesUrlWithLoginToken
        };

        await rpc.InvokeWithCancellationAsync(
            "displayDashboardUrls",
            [_token, dashboardUrlsState],
            cancellationToken);
    }

    public async Task ShowStatusAsync(string? status, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent status update: {Status}", status);

        await rpc.InvokeWithCancellationAsync(
            "showStatus",
            [_token, status],
            cancellationToken);
    }

    public async Task<T?> PromptForSelectionAsync<T>(string promptText, IEnumerable<T> choices, Func<T, string> choiceFormatter,
        CancellationToken cancellationToken) where T : notnull
    {
        await ConnectAsync(cancellationToken);

        var choicesList = choices.ToList();
        // this will throw if formatting results in non-distinct values. that should happen because we cannot send the formatter over the wire.
        var choicesByFormattedValue = choicesList.ToDictionary(choice => choiceFormatter(choice).RemoveSpectreFormatting(), choice => choice);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Prompting for selection with text: {PromptText}, choices: {Choices}", promptText, choicesByFormattedValue.Keys);

        var choicesArray = choicesByFormattedValue.Keys.ToArray();
        var result = await rpc.InvokeWithCancellationAsync<string?>(
            "promptForSelection",
            [_token, promptText, choicesArray],
            cancellationToken);

        return result is null
            ? default
            : choicesByFormattedValue[result];
    }

    public async Task<bool?> ConfirmAsync(string promptText, bool defaultValue, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Prompting for confirmation with text: {PromptText}, default value: {DefaultValue}", promptText, defaultValue);

        var result = await rpc.InvokeWithCancellationAsync<bool?>(
            "confirm",
            [_token, promptText, defaultValue],
            cancellationToken);

        return result;
    }

    public async Task<string?> PromptForStringAsync(string promptText, string? defaultValue, Func<string, ValidationResult>? validator,
        bool required, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        target.ValidationFunction = validator;

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Prompting for string with text: {PromptText}, default value: {DefaultValue}, required: {Required}", promptText, defaultValue, required);

        var result = await rpc.InvokeWithCancellationAsync<string?>(
            "promptForString",
            [_token, promptText, defaultValue, required],
            cancellationToken);

        return result;
    }

    public async Task OpenInIdeAsync(string path, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Opening path: {ProjectPath}", path);

        await rpc.InvokeWithCancellationAsync(
            "openInIde",
            [_token, path],
            cancellationToken);
    }

    public async Task LogMessageAsync(LogLevel logLevel, string message, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Logging message at level {LogLevel}: {Message}", logLevel, message);

        await rpc.InvokeWithCancellationAsync(
            "logMessage",
            [_token, logLevel.ToString(), message],
            cancellationToken);
    }

    public async Task DisplayPlainTextAsync(string text, CancellationToken cancellationToken)
    {
        await ConnectAsync(cancellationToken);

        using var activity = _activitySource.StartActivity();

        var rpc = await _rpcTaskCompletionSource.Task;

        logger.LogDebug("Sent plain text message {Message}", text);

        await rpc.InvokeWithCancellationAsync(
            "displayPlainText",
            [_token, text],
            cancellationToken);
    }

    private X509Certificate2 GetCertificate()
    {
        var serverCertificate = configuration[KnownConfigNames.ExtensionCert];
        Debug.Assert(!string.IsNullOrEmpty(serverCertificate));
        var data = Convert.FromBase64String(serverCertificate);
        return new X509Certificate2(data);
    }
}
