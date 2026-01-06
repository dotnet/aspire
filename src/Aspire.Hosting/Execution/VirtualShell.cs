// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace Aspire.Hosting.Execution;

/// <summary>
/// Default implementation of <see cref="IVirtualShell"/> that provides portable
/// command execution without invoking a shell.
/// </summary>
[Experimental("ASPIREHOSTINGVIRTUALSHELL001", UrlFormat = "https://aka.ms/dotnet/aspire/diagnostics#{0}")]
public sealed class VirtualShell : IVirtualShell
{
    private readonly ShellState _state;
    private readonly string? _tag;
    private readonly ICommandLineParser _parser;
    private readonly IExecutableResolver _resolver;
    private readonly IProcessRunner _runner;

    // Secrets
    private readonly Dictionary<string, string> _secrets;
    private readonly HashSet<string> _secretValues;
    private readonly HashSet<string> _secretEnvKeys;

    // Diagnostics
    private readonly bool _loggingEnabled;
    private readonly ILogger _logger;
    private readonly VirtualShellActivitySource _activitySource;
    private readonly Histogram<double> _durationHistogram;
    private readonly Counter<long> _commandCounter;

    // Internal accessors for Command class
    internal ShellState State => _state;
    internal string? CurrentTag => _tag;
    internal IProcessRunner Runner => _runner;
    internal bool LoggingEnabled => _loggingEnabled;
    internal ILogger Logger => _logger;
    internal VirtualShellActivitySource ActivitySource => _activitySource;

    /// <summary>
    /// Creates a new VirtualShell instance with the required dependencies.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="activitySource">The activity source for tracing.</param>
    /// <param name="meterFactory">The meter factory for metrics.</param>
    public VirtualShell(
        ILoggerFactory loggerFactory,
        VirtualShellActivitySource activitySource,
        IMeterFactory meterFactory)
        : this(
            ShellState.Default,
            tag: null,
            new CommandLineParser(),
            new ExecutableResolver(),
            new ProcessRunner(),
            secrets: new Dictionary<string, string>(StringComparer.Ordinal),
            secretValues: new HashSet<string>(StringComparer.Ordinal),
            secretEnvKeys: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            loggingEnabled: false,
            loggerFactory.CreateLogger<VirtualShell>(),
            activitySource,
            meterFactory)
    {
    }

    /// <summary>
    /// Creates a new VirtualShell instance with the specified dependencies.
    /// </summary>
    internal VirtualShell(
        ICommandLineParser parser,
        IExecutableResolver resolver,
        IProcessRunner runner,
        ILoggerFactory loggerFactory,
        VirtualShellActivitySource activitySource,
        IMeterFactory meterFactory)
        : this(
            ShellState.Default,
            tag: null,
            parser,
            resolver,
            runner,
            secrets: new Dictionary<string, string>(StringComparer.Ordinal),
            secretValues: new HashSet<string>(StringComparer.Ordinal),
            secretEnvKeys: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            loggingEnabled: false,
            loggerFactory.CreateLogger<VirtualShell>(),
            activitySource,
            meterFactory)
    {
    }

    private VirtualShell(
        ShellState state,
        string? tag,
        ICommandLineParser parser,
        IExecutableResolver resolver,
        IProcessRunner runner,
        Dictionary<string, string> secrets,
        HashSet<string> secretValues,
        HashSet<string> secretEnvKeys,
        bool loggingEnabled,
        ILogger logger,
        VirtualShellActivitySource activitySource,
        IMeterFactory meterFactory)
    {
        _state = state;
        _tag = tag;
        _parser = parser;
        _resolver = resolver;
        _runner = runner;
        _secrets = secrets;
        _secretValues = secretValues;
        _secretEnvKeys = secretEnvKeys;
        _loggingEnabled = loggingEnabled;
        _logger = logger;
        _activitySource = activitySource;

        var meter = meterFactory.Create("Aspire.VirtualShell");
        _durationHistogram = meter.CreateHistogram<double>(
            "virtualshell.command.duration",
            unit: "ms",
            description: "Duration of command execution");
        _commandCounter = meter.CreateCounter<long>(
            "virtualshell.command.count",
            description: "Number of commands executed");
    }

    // Private constructor for cloning with updated state
    private VirtualShell(
        ShellState state,
        string? tag,
        ICommandLineParser parser,
        IExecutableResolver resolver,
        IProcessRunner runner,
        Dictionary<string, string> secrets,
        HashSet<string> secretValues,
        HashSet<string> secretEnvKeys,
        bool loggingEnabled,
        ILogger logger,
        VirtualShellActivitySource activitySource,
        Histogram<double> durationHistogram,
        Counter<long> commandCounter)
    {
        _state = state;
        _tag = tag;
        _parser = parser;
        _resolver = resolver;
        _runner = runner;
        _secrets = secrets;
        _secretValues = secretValues;
        _secretEnvKeys = secretEnvKeys;
        _loggingEnabled = loggingEnabled;
        _logger = logger;
        _activitySource = activitySource;
        _durationHistogram = durationHistogram;
        _commandCounter = commandCounter;
    }

    private VirtualShell Clone(
        ShellState? state = null,
        string? tag = null,
        Dictionary<string, string>? secrets = null,
        HashSet<string>? secretValues = null,
        HashSet<string>? secretEnvKeys = null,
        bool? loggingEnabled = null)
    {
        return new VirtualShell(
            state ?? _state,
            tag ?? _tag,
            _parser,
            _resolver,
            _runner,
            secrets ?? _secrets,
            secretValues ?? _secretValues,
            secretEnvKeys ?? _secretEnvKeys,
            loggingEnabled ?? _loggingEnabled,
            _logger,
            _activitySource,
            _durationHistogram,
            _commandCounter);
    }

    /// <inheritdoc />
    public IVirtualShell Cd(string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        return Clone(state: _state with { WorkingDirectory = Path.GetFullPath(workingDirectory) });
    }

    /// <inheritdoc />
    public IVirtualShell Env(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return Clone(state: _state.WithEnv(key, value));
    }

    /// <inheritdoc />
    public IVirtualShell Env(IReadOnlyDictionary<string, string?> vars)
    {
        ArgumentNullException.ThrowIfNull(vars);
        return Clone(state: _state.WithEnv(vars));
    }

    /// <inheritdoc />
    public IVirtualShell PrependPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var currentPath = GetCurrentPath();
        var newPath = string.IsNullOrEmpty(currentPath)
            ? path
            : $"{path}{Path.PathSeparator}{currentPath}";
        return Env("PATH", newPath);
    }

    /// <inheritdoc />
    public IVirtualShell AppendPath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var currentPath = GetCurrentPath();
        var newPath = string.IsNullOrEmpty(currentPath)
            ? path
            : $"{currentPath}{Path.PathSeparator}{path}";
        return Env("PATH", newPath);
    }

    private string? GetCurrentPath()
    {
        // Check shell state environment first, then system environment
        return _state.Environment.TryGetValue("PATH", out var statePath)
            ? statePath
            : Environment.GetEnvironmentVariable("PATH");
    }

    /// <inheritdoc />
    public IVirtualShell DefineSecret(string name, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        var newSecrets = new Dictionary<string, string>(_secrets, StringComparer.Ordinal)
        {
            [name] = value
        };
        var newSecretValues = new HashSet<string>(_secretValues, StringComparer.Ordinal)
        {
            value
        };

        return Clone(secrets: newSecrets, secretValues: newSecretValues);
    }

    /// <inheritdoc />
    public string Secret(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (!_secrets.TryGetValue(name, out var value))
        {
            throw new KeyNotFoundException($"Secret '{name}' is not defined. Use DefineSecret() first.");
        }

        return value;
    }

    /// <inheritdoc />
    public IVirtualShell SecretEnv(string key, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(value);

        var newSecretEnvKeys = new HashSet<string>(_secretEnvKeys, StringComparer.OrdinalIgnoreCase)
        {
            key
        };
        var newSecretValues = new HashSet<string>(_secretValues, StringComparer.Ordinal)
        {
            value
        };

        return Clone(
            state: _state.WithEnv(key, value),
            secretEnvKeys: newSecretEnvKeys,
            secretValues: newSecretValues);
    }

    /// <inheritdoc />
    public IVirtualShell Tag(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return Clone(tag: category);
    }

    /// <inheritdoc />
    public IVirtualShell WithLogging()
    {
        return Clone(loggingEnabled: true);
    }

    /// <summary>
    /// Redacts secret values from a string.
    /// </summary>
    internal string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value ?? string.Empty;
        }

        if (_secretValues.Contains(value))
        {
            return "[REDACTED]";
        }

        return value;
    }

    /// <summary>
    /// Redacts secret values from arguments.
    /// </summary>
    internal IReadOnlyList<string> RedactArgs(IReadOnlyList<string> args)
    {
        var redacted = new string[args.Count];
        for (var i = 0; i < args.Count; i++)
        {
            redacted[i] = Redact(args[i]);
        }
        return redacted;
    }

    // ═══════════════════════════════════════════════════════════════════
    // Command creation
    // ═══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public ICommand Command(string commandLine)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(commandLine);
        var (fileName, args) = _parser.Parse(commandLine);
        var exePath = _resolver.ResolveOrThrow(fileName, _state);
        return new Command(this, fileName, args, exePath);
    }

    /// <inheritdoc />
    public ICommand Command(string fileName, IReadOnlyList<string>? args)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        args ??= Array.Empty<string>();
        var exePath = _resolver.ResolveOrThrow(fileName, _state);
        return new Command(this, fileName, args, exePath);
    }

    internal void RecordMetrics(string fileName, ProcessResult result, double durationMs)
    {
        var tags = new TagList
        {
            { "command", fileName },
            { "exit_code", result.ExitCode },
            { "success", result.Success }
        };

        if (_tag is not null)
        {
            tags.Add("tag", _tag);
        }

        _durationHistogram.Record(durationMs, tags);
        _commandCounter.Add(1, tags);
    }

}
