// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.VirtualShell.Internal;

namespace Aspire.Hosting.VirtualShell;

/// <summary>
/// Default implementation of <see cref="IVirtualShell"/> that provides portable
/// command execution without invoking a shell.
/// </summary>
public sealed class VirtualShell : IVirtualShell
{
    private readonly ShellState _state;
    private readonly string? _tag;
    private readonly ICommandLineParser _parser;
    private readonly IExecutableResolver _resolver;
    private readonly IProcessRunner _runner;

    /// <summary>
    /// Creates a new VirtualShell instance with default settings.
    /// </summary>
    public VirtualShell()
        : this(ShellState.Default, null, new CommandLineParser(), new ExecutableResolver(), new ProcessRunner())
    {
    }

    /// <summary>
    /// Creates a new VirtualShell instance with the specified dependencies.
    /// </summary>
    /// <param name="parser">The command line parser.</param>
    /// <param name="resolver">The executable resolver.</param>
    /// <param name="runner">The process runner.</param>
    internal VirtualShell(
        ICommandLineParser parser,
        IExecutableResolver resolver,
        IProcessRunner runner)
        : this(ShellState.Default, null, parser, resolver, runner)
    {
    }

    private VirtualShell(
        ShellState state,
        string? tag,
        ICommandLineParser parser,
        IExecutableResolver resolver,
        IProcessRunner runner)
    {
        _state = state;
        _tag = tag;
        _parser = parser;
        _resolver = resolver;
        _runner = runner;
    }

    /// <inheritdoc />
    public IVirtualShell Cd(string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        return new VirtualShell(
            _state with { WorkingDirectory = Path.GetFullPath(workingDirectory) },
            _tag,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public IVirtualShell Env(string key, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        return new VirtualShell(
            _state.WithEnv(key, value),
            _tag,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public IVirtualShell Env(IReadOnlyDictionary<string, string?> vars)
    {
        ArgumentNullException.ThrowIfNull(vars);
        return new VirtualShell(
            _state.WithEnv(vars),
            _tag,
            _parser,
            _resolver,
            _runner);
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
    public IVirtualShell Tag(string category)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(category);
        return new VirtualShell(
            _state,
            category,
            _parser,
            _resolver,
            _runner);
    }

    /// <inheritdoc />
    public ICommand Command(string commandLine)
    {
        var (fileName, args) = _parser.Parse(commandLine);
        return Command(fileName, args);
    }

    /// <inheritdoc />
    public ICommand Command(string fileName, IReadOnlyList<string> args)
    {
        return new Command(_state, _resolver, _runner, fileName, args);
    }

    /// <inheritdoc />
    public Task<CliResult> Run(string commandLine, CancellationToken ct = default)
    {
        return Command(commandLine).RunAsync(ct);
    }

    /// <inheritdoc />
    public Task<CliResult> Run(string fileName, IReadOnlyList<string> args, CancellationToken ct = default)
    {
        return Command(fileName, args).RunAsync(ct);
    }

    /// <inheritdoc />
    public IRunningProcess Start(string commandLine)
    {
        return Command(commandLine).WithCaptureOutput(false).Start();
    }

    /// <inheritdoc />
    public IRunningProcess Start(string fileName, IReadOnlyList<string> args)
    {
        return Command(fileName, args).WithCaptureOutput(false).Start();
    }
}
