// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace QuarantineTools.Tests;

public class CliParsingBehaviorTests
{
    private sealed record Parsed(string Mode, string? Url, string? Root, string Attribute, List<string> Tests);

    // Parser logic mirrored from tools/QuarantineTools/Quarantine.cs to validate contract.
    private static (bool ok, string? error, Parsed? result) Parse(string[] args)
    {
        bool quarantine = false;
        bool unquarantine = false;
        string? issueUrl = null;
        string? scanRoot = null;
        string attributeFullName = "Aspire.TestUtilities.QuarantinedTest";
        var tests = new List<string>();
        bool capturingTests = false;

        for (int i = 0; i < args.Length; i++)
        {
            var a = args[i];
            switch (a)
            {
                case "-q":
                case "--quarantine":
                    if (quarantine || unquarantine)
                    {
                        return (false, "Specify exactly one of -q/--quarantine or -u/--unquarantine (not multiple).", null);
                    }
                    quarantine = true;
                    capturingTests = true;
                    break;
                case "-u":
                case "--unquarantine":
                    if (quarantine || unquarantine)
                    {
                        return (false, "Specify exactly one of -q/--quarantine or -u/--unquarantine (not multiple).", null);
                    }
                    unquarantine = true;
                    capturingTests = true;
                    break;
                case "-i":
                case "--url":
                    if (i + 1 >= args.Length)
                    {
                        return (false, "Missing value for --url/-i.", null);
                    }
                    issueUrl = args[++i];
                    capturingTests = false;
                    break;
                case "-r":
                case "--root":
                    if (i + 1 >= args.Length)
                    {
                        return (false, "Missing value for --root/-r.", null);
                    }
                    scanRoot = args[++i];
                    capturingTests = false;
                    break;
                case "-a":
                case "--attribute":
                    if (i + 1 >= args.Length)
                    {
                        return (false, "Missing value for --attribute/-a.", null);
                    }
                    attributeFullName = args[++i];
                    capturingTests = false;
                    break;
                default:
                    if (a.StartsWith('-'))
                    {
                        return (false, $"Unknown option '{a}'.", null);
                    }
                    if (!quarantine && !unquarantine)
                    {
                        return (false, "Specify -q or -u before listing test names.", null);
                    }
                    if (!capturingTests)
                    {
                        return (false, "Test names must appear immediately after -q or -u, before other options.", null);
                    }
                    tests.Add(a);
                    break;
            }
        }

        if (quarantine == unquarantine)
        {
            return (false, "Specify exactly one of -q/--quarantine or -u/--unquarantine.", null);
        }
        if (tests.Count == 0)
        {
            return (false, "Specify at least one fully-qualified test method name.", null);
        }

        var mode = quarantine ? "quarantine" : "unquarantine";
        if (mode == "quarantine" && string.IsNullOrWhiteSpace(issueUrl))
        {
            return (false, "Quarantining requires an issue URL (--url or -i).", null);
        }

        return (true, null, new Parsed(mode, issueUrl, scanRoot, attributeFullName, tests));
    }

    [Fact]
    public void Valid_Quarantine_TestsImmediatelyAfterQ()
    {
        var (ok, err, result) = Parse(new[] { "-q", "N.C.M", "-i", "https://github.com/org/repo/issues/1" });
        Assert.True(ok, err);
        Assert.Equal("quarantine", result!.Mode);
        Assert.Equal(["N.C.M"], result.Tests);
        Assert.Equal("https://github.com/org/repo/issues/1", result.Url);
    }

    [Fact]
    public void Invalid_TestBeforeMode_IsRejected()
    {
        var (ok, err, _) = Parse(new[] { "N.C.M", "-q", "-i", "https://github.com/org/repo/issues/1" });
        Assert.False(ok);
        Assert.Contains("Specify -q or -u before listing test names.", err);
    }

    [Fact]
    public void Invalid_TestAfterOptions_IsRejected()
    {
        var (ok, err, _) = Parse(new[] { "-q", "-i", "https://github.com/org/repo/issues/1", "N.C.M" });
        Assert.False(ok);
        Assert.Contains("Test names must appear immediately after -q or -u", err);
    }

    [Fact]
    public void Valid_Unquarantine_TestsImmediatelyAfterU_NoUrlRequired()
    {
        var (ok, err, result) = Parse(new[] { "-u", "N.C.M" });
        Assert.True(ok, err);
        Assert.Equal("unquarantine", result!.Mode);
        Assert.Equal(["N.C.M"], result.Tests);
        Assert.Null(result.Url);
    }
}
