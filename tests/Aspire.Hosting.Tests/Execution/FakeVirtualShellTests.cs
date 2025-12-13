// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREHOSTINGVIRTUALSHELL001

using Aspire.Hosting.Execution;

namespace Aspire.Hosting.Tests.Execution;

/// <summary>
/// Tests for <see cref="FakeVirtualShell"/> which captures commands for testing.
/// </summary>
public class FakeVirtualShellTests
{
    #region Basic Command Execution

    [Fact]
    public async Task Run_CapturesCommand()
    {
        var shell = new FakeVirtualShell();

        await shell.RunAsync("dotnet", ["build"]);

        var command = Assert.Single(shell.ExecutedCommands);
        Assert.Equal("dotnet", command.FileName);
        Assert.Equal(["build"], command.Arguments);
    }

    [Fact]
    public async Task Run_CommandLine_ParsesCommand()
    {
        var shell = new FakeVirtualShell();

        await shell.RunAsync("dotnet build");

        var command = Assert.Single(shell.ExecutedCommands);
        Assert.Equal("dotnet", command.FileName);
        Assert.Equal(["build"], command.Arguments);
    }

    [Fact]
    public async Task Run_ReturnsDefaultResult()
    {
        var shell = new FakeVirtualShell();

        var result = await shell.RunAsync("any-command");

        Assert.True(result.Success);
        Assert.Equal(0, result.ExitCode);
    }

    [Fact]
    public async Task WithDefaultResult_ReturnsConfiguredResult()
    {
        var shell = new FakeVirtualShell()
            .WithDefaultResult(new ProcessResult(42, "output", "error", ProcessExitReason.Exited));

        var result = await shell.RunAsync("any-command");

        Assert.False(result.Success);
        Assert.Equal(42, result.ExitCode);
        Assert.Equal("output", result.Stdout);
        Assert.Equal("error", result.Stderr);
    }

    [Fact]
    public async Task WithResponse_ReturnsConfiguredResultForCommand()
    {
        var shell = new FakeVirtualShell()
            .WithResponse("dotnet", new ProcessResult(0, "Build succeeded", "", ProcessExitReason.Exited))
            .WithResponse("npm", new ProcessResult(1, "", "npm ERR!", ProcessExitReason.Exited));

        var dotnetResult = await shell.RunAsync("dotnet", ["build"]);
        var npmResult = await shell.RunAsync("npm", ["install"]);

        Assert.True(dotnetResult.Success);
        Assert.Equal("Build succeeded", dotnetResult.Stdout);

        Assert.False(npmResult.Success);
        Assert.Equal("npm ERR!", npmResult.Stderr);
    }

    [Fact]
    public async Task WithResponseHandler_CallsHandlerWithCommand()
    {
        var shell = new FakeVirtualShell()
            .WithResponseHandler("echo", cmd =>
            {
                var text = string.Join(" ", cmd.Arguments);
                return new ProcessResult(0, text, "", ProcessExitReason.Exited);
            });

        var result = await shell.RunAsync("echo", ["hello", "world"]);

        Assert.Equal("hello world", result.Stdout);
    }

    #endregion

    #region Working Directory

    [Fact]
    public async Task Cd_SetsWorkingDirectory()
    {
        var shell = new FakeVirtualShell();
        var shellWithCd = shell.Cd("/app") as FakeVirtualShell;

        await shellWithCd!.RunAsync("ls");

        var command = shellWithCd.ExecutedCommands.Single();
        Assert.Equal("/app", command.WorkingDirectory);
    }

    [Fact]
    public void Cd_CreatesNewInstance()
    {
        var shell = new FakeVirtualShell();
        var shellWithCd = shell.Cd("/app");

        Assert.NotSame(shell, shellWithCd);
        Assert.Null(shell.WorkingDirectory);
        Assert.Equal("/app", (shellWithCd as FakeVirtualShell)?.WorkingDirectory);
    }

    [Fact]
    public async Task Cd_Chained_UsesLastDirectory()
    {
        var shell = new FakeVirtualShell()
            .Cd("/first")
            .Cd("/second") as FakeVirtualShell;

        await shell!.RunAsync("pwd");

        var command = shell.ExecutedCommands.Single();
        Assert.Equal("/second", command.WorkingDirectory);
    }

    #endregion

    #region Environment Variables

    [Fact]
    public async Task Env_SetsEnvironmentVariable()
    {
        var shell = new FakeVirtualShell()
            .Env("NODE_ENV", "production") as FakeVirtualShell;

        await shell!.RunAsync("node", ["app.js"]);

        var command = shell.ExecutedCommands.Single();
        Assert.Equal("production", command.Environment["NODE_ENV"]);
    }

    [Fact]
    public async Task Env_NullValue_RemovesVariable()
    {
        var shell = new FakeVirtualShell()
            .Env("VAR", "value")
            .Env("VAR", null) as FakeVirtualShell;

        await shell!.RunAsync("test");

        var command = shell.ExecutedCommands.Single();
        Assert.False(command.Environment.ContainsKey("VAR"));
    }

    [Fact]
    public async Task Env_Dictionary_SetsMultipleVariables()
    {
        var shell = new FakeVirtualShell()
            .Env(new Dictionary<string, string?>
            {
                ["VAR1"] = "value1",
                ["VAR2"] = "value2"
            }) as FakeVirtualShell;

        await shell!.RunAsync("test");

        var command = shell.ExecutedCommands.Single();
        Assert.Equal("value1", command.Environment["VAR1"]);
        Assert.Equal("value2", command.Environment["VAR2"]);
    }

    [Fact]
    public void Env_CreatesNewInstance()
    {
        var shell = new FakeVirtualShell();
        var shellWithEnv = shell.Env("VAR", "value");

        Assert.NotSame(shell, shellWithEnv);
        Assert.Empty(shell.Environment);
    }

    #endregion

    #region PATH Manipulation

    [Fact]
    public async Task PrependPath_AddsToFrontOfPath()
    {
        var shell = new FakeVirtualShell()
            .PrependPath("/custom/bin") as FakeVirtualShell;

        await shell!.RunAsync("test");

        var command = shell.ExecutedCommands.Single();
        Assert.StartsWith("/custom/bin", command.Environment["PATH"]);
    }

    [Fact]
    public async Task AppendPath_AddsToEndOfPath()
    {
        var shell = new FakeVirtualShell()
            .AppendPath("/custom/bin") as FakeVirtualShell;

        await shell!.RunAsync("test");

        var command = shell.ExecutedCommands.Single();
        Assert.EndsWith("/custom/bin", command.Environment["PATH"]);
    }

    #endregion

    #region Secrets

    [Fact]
    public void DefineSecret_And_Secret_WorkTogether()
    {
        var shell = new FakeVirtualShell()
            .DefineSecret("API_KEY", "secret123");

        var value = shell.Secret("API_KEY");

        Assert.Equal("secret123", value);
    }

    [Fact]
    public void Secret_ThrowsForUndefinedSecret()
    {
        var shell = new FakeVirtualShell();

        Assert.Throws<KeyNotFoundException>(() => shell.Secret("UNDEFINED"));
    }

    [Fact]
    public async Task SecretEnv_SetsEnvironmentVariable()
    {
        var shell = new FakeVirtualShell()
            .SecretEnv("API_KEY", "secret123") as FakeVirtualShell;

        await shell!.RunAsync("test");

        var command = shell.ExecutedCommands.Single();
        Assert.Equal("secret123", command.Environment["API_KEY"]);
    }

    #endregion

    #region Tags

    [Fact]
    public void Tag_SetsCategory()
    {
        var shell = new FakeVirtualShell()
            .Tag("build") as FakeVirtualShell;

        Assert.Equal("build", shell!.CurrentTag);
    }

    [Fact]
    public void Tag_CreatesNewInstance()
    {
        var shell = new FakeVirtualShell();
        var shellWithTag = shell.Tag("deploy");

        Assert.NotSame(shell, shellWithTag);
        Assert.Null(shell.CurrentTag);
    }

    #endregion

    #region Command Builder

    [Fact]
    public async Task Command_WithStdin_PassesStdin()
    {
        var shell = new FakeVirtualShell();
        var stdin = Stdin.FromText("input data");

        await shell.Command("cat")
            .WithStdin(stdin)
            .RunAsync();

        var command = shell.ExecutedCommands.Single();
        Assert.NotNull(command.Stdin);
    }

    [Fact]
    public async Task Command_WithCaptureOutputFalse_SetsFlag()
    {
        var shell = new FakeVirtualShell();

        await shell.Command("echo", ["test"])
            .WithCaptureOutput(false)
            .RunAsync();

        var command = shell.ExecutedCommands.Single();
        Assert.False(command.CaptureOutput);
    }

    #endregion

    #region Start (Streaming)

    [Fact]
    public async Task Start_ReturnsRunningProcess()
    {
        var shell = new FakeVirtualShell()
            .WithResponse("echo", new ProcessResult(0, "line1\nline2", "", ProcessExitReason.Exited));

        await using var process = shell.Start("echo", ["hello"]);
        var lines = new List<string>();

        await foreach (var line in process.ReadLinesAsync())
        {
            lines.Add(line.Text);
        }

        Assert.Equal(["line1", "line2"], lines);
    }

    [Fact]
    public async Task Start_WaitAsync_ReturnsResult()
    {
        var shell = new FakeVirtualShell()
            .WithResponse("test", new ProcessResult(42, "", "", ProcessExitReason.Exited));

        await using var process = shell.Start("test");
        var result = await process.WaitAsync();

        Assert.Equal(42, result.ExitCode);
    }

    [Fact]
    public async Task Start_EnsureSuccessAsync_ThrowsOnFailure()
    {
        var shell = new FakeVirtualShell()
            .WithResponse("fail", new ProcessResult(1, "", "error message", ProcessExitReason.Exited));

        await using var process = shell.Start("fail");

        await Assert.ThrowsAsync<InvalidOperationException>(() => process.EnsureSuccessAsync());
    }

    #endregion

    #region Immutability

    [Fact]
    public void AllMethods_ReturnNewInstances()
    {
        var shell = new FakeVirtualShell();

        var shell1 = shell.Cd("/app");
        var shell2 = shell.Env("VAR", "value");
        var shell3 = shell.PrependPath("/bin");
        var shell4 = shell.AppendPath("/bin");
        var shell5 = shell.DefineSecret("KEY", "value");
        var shell6 = shell.SecretEnv("KEY", "value");
        var shell7 = shell.Tag("build");
        var shell8 = shell.WithLogging();

        Assert.NotSame(shell, shell1);
        Assert.NotSame(shell, shell2);
        Assert.NotSame(shell, shell3);
        Assert.NotSame(shell, shell4);
        Assert.NotSame(shell, shell5);
        Assert.NotSame(shell, shell6);
        Assert.NotSame(shell, shell7);
        Assert.NotSame(shell, shell8);
    }

    [Fact]
    public async Task SharedCommandQueue_AcrossInstances()
    {
        var shell = new FakeVirtualShell();
        var shell2 = shell.Cd("/app") as FakeVirtualShell;

        await shell.RunAsync("cmd1");
        await shell2!.RunAsync("cmd2");

        // Both instances share the same command queue
        Assert.Equal(2, shell.ExecutedCommands.Count);
        Assert.Equal(2, shell2.ExecutedCommands.Count);
    }

    #endregion

    #region ClearCommands

    [Fact]
    public async Task ClearCommands_RemovesAllCapturedCommands()
    {
        var shell = new FakeVirtualShell();

        await shell.RunAsync("cmd1");
        await shell.RunAsync("cmd2");
        Assert.Equal(2, shell.ExecutedCommands.Count);

        shell.ClearCommands();

        Assert.Empty(shell.ExecutedCommands);
    }

    #endregion

    #region GetStateAsJson

    [Fact]
    public void GetStateAsJson_ReturnsShellState()
    {
        var shell = new FakeVirtualShell()
            .Cd("/app")
            .Env("VAR", "value")
            .Tag("build") as FakeVirtualShell;

        var json = shell!.GetStateAsJson();

        Assert.Equal("/app", json["workingDirectory"]?.GetValue<string>());
        Assert.Equal("value", json["environment"]?["VAR"]?.GetValue<string>());
        Assert.Equal("build", json["tag"]?.GetValue<string>());
    }

    #endregion
}
