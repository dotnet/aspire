import * as fs from 'fs';
import * as path from 'path';
import * as vscode from 'vscode';
import { randomUUID } from 'crypto';
import { spawnSync } from 'child_process';
import { createRequire } from 'module';

// Investigation-only instrumentation for #20117; not part of the proposed fix.
export function installWasmInvestigationProbe(logDirectory: string): vscode.Disposable {
    fs.mkdirSync(logDirectory, { recursive: true });
    const logPath = path.join(logDirectory, `wasm-investigation-${randomUUID()}.jsonl`);
    const record = (event: string, session: vscode.DebugSession | undefined, detail?: unknown) => {
        fs.appendFileSync(logPath, `${JSON.stringify({
            observedAt: new Date().toISOString(),
            event,
            sessionId: session?.id,
            sessionType: session?.type,
            sessionName: session?.name,
            parentSessionId: session?.parentSession?.id,
            detail,
        }, (key, value) => /env|token|password|secret|authorization/i.test(key) ? '<redacted>' : value)}\n`);
    };
    const lifecycleCommands = new Set(['initialize', 'launch', 'attach', 'configurationDone', 'disconnect', 'terminate']);
    const lifecycleEvents = new Set(['initialized', 'process', 'exited', 'terminated', 'thread', 'stopped', 'continued']);
    record('probe-installed', undefined, { platform: process.platform, architecture: process.arch, vscode: vscode.version });
    const bridgeExceptionProbe = installBridgeExceptionProbe(logDirectory);
    const nodeOpeningProbe = installNodeOpeningProbe(logDirectory);
    record('bridge-exception-probe-installed', undefined);

    return vscode.Disposable.from(
        bridgeExceptionProbe,
        nodeOpeningProbe,
        vscode.debug.registerDebugConfigurationProvider('*', {
            resolveDebugConfigurationWithSubstitutedVariables(_folder, configuration) {
                if (configuration.type === 'monovsdbg_wasm') {
                    // The pinned adapter deprecates engineLogging in favor of diagnosticsLog.
                    // Keep full DAP payload logging off: the field-selected tracker below already
                    // captures teardown requests without printing launch environments.
                    configuration.logging = {
                        ...configuration.logging,
                        engineLogging: false,
                        diagnosticsLog: {
                            ...configuration.logging?.diagnosticsLog,
                            protocolMessages: false,
                            dispatcherMessages: 'normal',
                            debugEngineAPITracing: 'all',
                            debugRuntimeEventTracing: true,
                            expressionEvaluationTracing: false,
                            startDebuggingTracing: true,
                        },
                    };
                    record('native-logging-enabled', undefined, {
                        type: configuration.type,
                        diagnosticsLog: configuration.logging.diagnosticsLog,
                    });
                }
                return configuration;
            },
        }),
        vscode.debug.onDidStartDebugSession(session => record('session-started', session)),
        vscode.debug.onDidTerminateDebugSession(session => record('session-terminated', session)),
        vscode.debug.registerDebugAdapterTrackerFactory('*', {
            createDebugAdapterTracker(session) {
                record('tracker-created', session);
                return {
                    onWillStartSession: () => record('adapter-will-start', session),
                    onWillStopSession: () => record('adapter-will-stop', session),
                    onError: error => record('adapter-error', session, { message: error.message, stack: error.stack }),
                    onExit: (code, signal) => record('adapter-exit', session, { code, signal }),
                    onWillReceiveMessage(message) {
                        if (message?.type === 'request' && lifecycleCommands.has(message.command)) {
                            record('client-to-adapter', session, {
                                sequence: message.seq,
                                command: message.command,
                                arguments: message.command === 'disconnect' || message.command === 'terminate'
                                    ? message.arguments
                                    : {
                                        program: message.arguments?.program,
                                        monoDebuggerOptions: message.arguments?.monoDebuggerOptions,
                                        diagnosticsLog: message.arguments?.logging?.diagnosticsLog,
                                    },
                            });
                        }
                    },
                    onDidSendMessage(message) {
                        if ((message?.type === 'response' && lifecycleCommands.has(message.command))
                            || (message?.type === 'event' && lifecycleEvents.has(message.event))
                            || (session.type === 'monovsdbg_wasm' && message?.type === 'event' && message.event === 'output')) {
                            record('adapter-to-client', session, {
                                sequence: message.seq,
                                requestSequence: message.request_seq,
                                type: message.type,
                                command: message.command,
                                eventName: message.event,
                                success: message.success,
                                message: message.message,
                                body: message.body,
                            });
                        }
                    },
                };
            },
        }),
        new vscode.Disposable(() => record('probe-disposed', undefined)),
    );
}

function installNodeOpeningProbe(logDirectory: string): vscode.Disposable {
    const hookPath = path.join(logDirectory, 'wasm-node-opening-probe.cjs');
    fs.writeFileSync(hookPath, nodeOpeningHookSource);
    const previousOptions = process.env.NODE_OPTIONS;
    const previousDirectory = process.env.ASPIRE_WASM_NODE_TRACE_DIRECTORY;
    const options = `${previousOptions ?? ''} --require ${JSON.stringify(hookPath)}`.trim();
    process.env.NODE_OPTIONS = options;
    process.env.ASPIRE_WASM_NODE_TRACE_DIRECTORY = logDirectory;
    // js-debug can run inline in this extension host, not just in a child adapter.
    const hook = createRequire(__filename)(hookPath) as { dispose(): void };
    return new vscode.Disposable(() => {
        hook.dispose();
        if (process.env.NODE_OPTIONS === options) {
            if (previousOptions === undefined) {
                delete process.env.NODE_OPTIONS;
            }
            else {
                process.env.NODE_OPTIONS = previousOptions;
            }
        }
        if (process.env.ASPIRE_WASM_NODE_TRACE_DIRECTORY === logDirectory) {
            if (previousDirectory === undefined) {
                delete process.env.ASPIRE_WASM_NODE_TRACE_DIRECTORY;
            }
            else {
                process.env.ASPIRE_WASM_NODE_TRACE_DIRECTORY = previousDirectory;
            }
        }
    });
}

// Observe js-debug's bundled WebSocket implementation through Node's HTTP boundary.
// No debugger binary, opening timeout, retry, request argument, or response is changed.
const nodeOpeningHookSource = `
const fs = require('node:fs');
const http = require('node:http');
const path = require('node:path');
const directory = process.env.ASPIRE_WASM_NODE_TRACE_DIRECTORY;
if (!directory) {
    throw new Error('Node opening investigation trace directory is missing.');
}
const file = path.join(directory, 'wasm-node-opening-' + process.pid + '.jsonl');
let count = 0;
let sequence = 0;
function record(event, detail) {
    if (++count <= 1024) {
        fs.appendFileSync(file, JSON.stringify({
            observedAt: new Date().toISOString(), processId: process.pid, event, detail
        }) + '\\n');
    } else if (count === 1025) {
        fs.appendFileSync(file, JSON.stringify({ event: 'opening-event-limit-reached', limit: 1024 }) + '\\n');
    }
}
record('hook-installed', { script: path.basename(process.argv[1] || ''), node: process.version });
const originalRequest = http.request;
const observedRequest = function (...args) {
    const request = Reflect.apply(originalRequest, this, args);
    // Only local debugger upgrades, e.g. /devtools/browser/<id>; never headers or CDP data.
    if (!/^(127\\.0\\.0\\.1|localhost|\\[::1\\])(:\\d+)?$/.test(String(request.getHeader('host')))
        || !/^\\/devtools\\/(browser|page)\\//.test(request.path)) {
        return request;
    }
    const id = ++sequence;
    const started = performance.now();
    const detail = () => ({
        id, elapsedMs: Math.round(performance.now() - started),
        path: request.path, host: request.getHeader('host'),
        localPort: request.socket?.localPort, remotePort: request.socket?.remotePort
    });
    record('opening-request', detail());
    request.once('socket', socket => {
        socket.once('connect', () => record('opening-connected', detail()));
    });
    request.once('upgrade', response => record('opening-upgrade', { ...detail(), statusCode: response.statusCode }));
    request.once('error', error => record('opening-error', { ...detail(), message: error.message }));
    request.once('close', () => record('opening-close', detail()));
    const originalDestroy = request.destroy;
    request.destroy = function (...destroyArgs) {
        record('opening-destroy', { ...detail(), stack: new Error().stack, message: destroyArgs[0]?.message });
        return Reflect.apply(originalDestroy, this, destroyArgs);
    };
    return request;
};
http.request = observedRequest;
module.exports = {
    dispose() {
        if (http.request === observedRequest) {
            http.request = originalRequest;
        }
    }
};
`;

function installBridgeExceptionProbe(logDirectory: string): vscode.Disposable {
    const directory = path.join(logDirectory, 'bridge-exception-probe');
    fs.mkdirSync(directory, { recursive: true });
    const projectPath = path.join(directory, 'BridgeExceptionProbe.csproj');
    fs.writeFileSync(projectPath, `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <LangVersion>13</LangVersion>
  </PropertyGroup>
</Project>
`);
    fs.writeFileSync(path.join(directory, 'StartupHook.cs'), bridgeStartupHookSource);
    const outputPath = path.join(directory, 'out');
    const build = spawnSync('dotnet', [
        'build', projectPath, '--configuration', 'Release', '--output', outputPath,
        '--verbosity', 'quiet', '--nologo',
        '-p:ImportDirectoryBuildProps=false', '-p:ImportDirectoryBuildTargets=false',
        '-p:NuGetAudit=false',
    ], {
        cwd: directory,
        encoding: 'utf8',
        windowsHide: true,
        timeout: 180000,
        env: { ...process.env, MSBUILDTERMINALLOGGER: 'false' },
    });
    fs.writeFileSync(path.join(directory, 'build.log'), `${build.stdout ?? ''}\n${build.stderr ?? ''}`);
    if (build.error || build.status !== 0) {
        throw new Error(`Could not build the investigation-only bridge exception probe: ${build.error?.message ?? `exit ${build.status}`}. See ${path.join(directory, 'build.log')}.`);
    }

    // Observe caught bridge exceptions without replacing its binaries or changing its
    // log filters, protocol traffic, retries, or timeouts. Compile against an older
    // runtime because C# can launch the net9 bridge on a different SDK's runtime.
    // https://github.com/dotnet/runtime/blob/main/docs/design/features/host-startup-hook.md
    const hookPath = path.join(outputPath, 'BridgeExceptionProbe.dll');
    const previousHooks = process.env.DOTNET_STARTUP_HOOKS;
    const previousDirectory = process.env.ASPIRE_WASM_BRIDGE_TRACE_DIRECTORY;
    const hooks = previousHooks ? `${previousHooks}${path.delimiter}${hookPath}` : hookPath;
    process.env.DOTNET_STARTUP_HOOKS = hooks;
    process.env.ASPIRE_WASM_BRIDGE_TRACE_DIRECTORY = logDirectory;
    return new vscode.Disposable(() => {
        if (process.env.DOTNET_STARTUP_HOOKS === hooks) {
            if (previousHooks === undefined) {
                delete process.env.DOTNET_STARTUP_HOOKS;
            }
            else {
                process.env.DOTNET_STARTUP_HOOKS = previousHooks;
            }
        }
        if (process.env.ASPIRE_WASM_BRIDGE_TRACE_DIRECTORY === logDirectory) {
            if (previousDirectory === undefined) {
                delete process.env.ASPIRE_WASM_BRIDGE_TRACE_DIRECTORY;
            }
            else {
                process.env.ASPIRE_WASM_BRIDGE_TRACE_DIRECTORY = previousDirectory;
            }
        }
    });
}

const bridgeStartupHookSource = `
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.IO;
using System.Reflection;
using System.Text.Json;

internal static class StartupHook
{
    [ThreadStatic]
    private static bool s_writing;

    private static EventListener? s_networkListener;
    private static IDisposable? s_diagnosticListener;
    private static IDisposable? s_requestListener;

    public static void Initialize()
    {
        // Other dotnet children inherit the environment but must not be traced.
        if (Assembly.GetEntryAssembly()?.GetName().Name != "Microsoft.Diagnostics.BrowserDebugHost")
        {
            return;
        }

        var directory = Environment.GetEnvironmentVariable("ASPIRE_WASM_BRIDGE_TRACE_DIRECTORY")
            ?? throw new InvalidOperationException("Bridge investigation trace directory is missing.");
        var file = Path.Combine(directory, $"wasm-bridge-exceptions-{Environment.ProcessId}-{Guid.NewGuid():N}.jsonl");
        var writer = new StreamWriter(new FileStream(file, FileMode.CreateNew, FileAccess.Write, FileShare.Read))
        {
            AutoFlush = true,
        };
        var gate = new object();
        var exceptionCount = 0;

        void Write(string eventName, object? detail = null)
        {
            writer.WriteLine(JsonSerializer.Serialize(new
            {
                observedAt = DateTimeOffset.UtcNow,
                processId = Environment.ProcessId,
                activityId = Activity.Current?.Id,
                threadId = Environment.CurrentManagedThreadId,
                eventName,
                detail,
            }));
        }

        Write("hook-installed", new { runtime = Environment.Version.ToString() });
        var networkEventCount = 0;
        var socketEventCount = 0;
        var requestEventCount = 0;
        s_diagnosticListener = DiagnosticListener.AllListeners.Subscribe(new DiagnosticObserver<DiagnosticListener>(listener =>
        {
            if (listener.Name != "Microsoft.AspNetCore")
            {
                return;
            }

            // Enabling the hosting activity links each IDE upgrade to the native
            // TCP connect executed by that same request, even across async awaits.
            s_requestListener = listener.Subscribe(new DiagnosticObserver<KeyValuePair<string, object?>>(args =>
            {
                lock (gate)
                {
                    if (++requestEventCount > 256)
                    {
                        if (requestEventCount == 257)
                        {
                            Write("http-request-event-limit-reached", new { limit = 256 });
                        }
                        return;
                    }

                    // Hosting events expose HttpContext through the payload's HttpContext
                    // property. Reflect metadata only; the hook has no ASP.NET dependency.
                    var context = ReadProperty(args.Value, "HttpContext");
                    var request = ReadProperty(context, "Request");
                    var connection = ReadProperty(context, "Connection");
                    Write("http-request-event", new
                    {
                        name = args.Key,
                        payloadType = args.Value?.GetType().FullName,
                        traceIdentifier = ReadProperty(context, "TraceIdentifier"),
                        path = ReadProperty(request, "Path")?.ToString(),
                        localPort = ReadProperty(connection, "LocalPort"),
                        remotePort = ReadProperty(connection, "RemotePort"),
                    });
                }
            }), name => name.StartsWith("Microsoft.AspNetCore.Hosting.HttpRequestIn", StringComparison.Ordinal));
        }));
        s_networkListener = new NetworkEventListener(args =>
        {
            lock (gate)
            {
                var isSocket = args.EventSource.Name != "Private.InternalDiagnostics.System.Net.WebSockets";
                var count = isSocket ? ++socketEventCount : ++networkEventCount;
                var limit = isSocket ? 2048 : 4096;
                var kind = isSocket ? "socket-event" : "websocket-event";
                if (count <= limit)
                {
                    var fields = new Dictionary<string, object?>();
                    for (var index = 0; index < (args.PayloadNames?.Count ?? 0); index++)
                    {
                        fields.Add(args.PayloadNames![index], args.Payload![index]);
                    }
                    Write(kind, new
                    {
                        source = args.EventSource.Name,
                        name = args.EventName,
                        eventActivityId = args.ActivityId,
                        fields,
                    });
                }
                else if (count == limit + 1)
                {
                    Write(kind + "-limit-reached", new { limit });
                }
            }
        });
        AppDomain.CurrentDomain.FirstChanceException += (_, args) =>
        {
            // A logging exception must not recursively re-enter this diagnostic handler.
            if (s_writing)
            {
                return;
            }
            s_writing = true;
            try
            {
                lock (gate)
                {
                    exceptionCount++;
                    if (exceptionCount <= 512)
                    {
                        Write("first-chance-exception", new
                        {
                            type = args.Exception.GetType().FullName,
                            message = args.Exception.Message,
                            stack = args.Exception.StackTrace,
                        });
                    }
                    else if (exceptionCount == 513)
                    {
                        Write("exception-limit-reached", new { limit = 512 });
                    }
                }
            }
            finally
            {
                s_writing = false;
            }
        };
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            lock (gate)
            {
                Write("process-exit", new { exceptionCount, networkEventCount, socketEventCount, requestEventCount });
            }
        };
    }

    private static object? ReadProperty(object? value, string name)
    {
        return value?.GetType().GetProperty(name)?.GetValue(value);
    }

    private sealed class DiagnosticObserver<T>(Action<T> next) : IObserver<T>
    {
        public void OnNext(T value) => next(value);
        public void OnError(Exception error) => throw error;
        public void OnCompleted() { }
    }

    private sealed class NetworkEventListener(Action<EventWrittenEventArgs> write) : EventListener
    {
        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name is "Private.InternalDiagnostics.System.Net.WebSockets"
                or "Private.InternalDiagnostics.System.Net.Sockets")
            {
                // Default keyword only: do not even enable the buffer-dump keyword.
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)1);
            }
            else if (eventSource.Name == "System.Net.Sockets")
            {
                EnableEvents(eventSource, EventLevel.Verbose, (EventKeywords)(-1));
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            if (eventData.EventSource.Name == "Private.InternalDiagnostics.System.Net.Sockets"
                && eventData.EventName == "Info"
                && eventData.Payload?[1] is string member
                && member is not (".ctor" or "Connect" or "DoConnect" or "SetToConnected"
                    or "Dispose" or "Close" or "SetToDisconnected" or "UpdateStatusAfterSocketError"))
            {
                return;
            }
            // Associate events link each WebSocket to its underlying stream, allowing
            // browser-client reads to be distinguished from IDE-server reads. Omit
            // payload dumps; this investigation needs connection lifecycle, not data.
            if (eventData.EventName is not null
                && !eventData.EventName.Contains("Dump", StringComparison.OrdinalIgnoreCase))
            {
                write(eventData);
            }
        }
    }
}
`;
