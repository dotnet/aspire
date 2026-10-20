// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.TestUtilities;
using VerifyXunit;
using Xunit;

namespace Infrastructure.Tests;

public sealed class NuxtDiagnosticsTests(ITestOutputHelper output)
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [RequiresTools(["bash", "tar", "jq"])]
    public async Task PreservesFailureAndFiltersUploadedDiagnostics(bool describeSucceeds)
    {
        using var workspace = TemporaryWorkspace.Create(output);
        var bin = Directory.CreateDirectory(Path.Combine(workspace.Path, "bin")).FullName;
        var home = Directory.CreateDirectory(Path.Combine(workspace.Path, "home")).FullName;
        var diagnostics = Path.Combine(workspace.Path, "diagnostics");
        var describeJson = Path.Combine(workspace.Path, "describe.json");
        var repository = Directory.CreateDirectory(Path.Combine(workspace.Path, "repository")).FullName;
        Directory.CreateDirectory(Path.Combine(repository, "src", "Aspire.Hosting.Nuxt"));
        Directory.CreateDirectory(Path.Combine(repository, "playground", "NuxtApp", "api"));
        Directory.CreateDirectory(Path.Combine(repository, "playground", "NuxtApp", "web"));

        await File.WriteAllTextAsync(describeJson, """
            {
              "extra": "root-secret",
              "resources": [
                {
                  "name": "api",
                  "displayName": "API",
                  "resourceType": "Executable",
                  "state": "Running",
                  "exitCode": null,
                  "healthStatus": "Healthy",
                  "source": "server.mjs",
                  "environment": { "API_KEY": "environment-secret" },
                  "properties": { "connectionString": "properties-secret" },
                  "commands": { "restart": { "description": "commands-secret" } },
                  "dashboardUrl": "http://localhost:1234/login?t=dashboard-secret",
                  "urls": [{ "name": "http", "url": "http://localhost/?token=url-secret" }],
                  "healthReports": { "http": { "description": "health-report-secret" } }
                },
                {
                  "name": "web-installer",
                  "displayName": "Web installer",
                  "resourceType": "Executable",
                  "state": "Exited",
                  "exitCode": 1,
                  "healthStatus": "Unhealthy",
                  "source": "npm"
                }
              ]
            }
            """);

        await File.WriteAllTextAsync(Path.Combine(bin, "aspire"), """
            #!/bin/bash
            case "$1" in
                start)
                    mkdir -p "$HOME/.aspire/logs"
                    printf 'startup evidence\nhttp://localhost:1234/login?t=secret-token\n' \
                        > "$HOME/.aspire/logs/cli_new_detach-child_new.log"
                    ;;
                describe)
                    [ "$TEST_DESCRIBE_SUCCEEDS" = true ] || exit 1
                    cat "$TEST_DESCRIBE_JSON"
                    ;;
                logs) printf '%s failure output\n' "$2" ;;
                stop) exit 19 ;;
                --version) printf 'test CLI\n' ;;
            esac
            """);
        await File.WriteAllTextAsync(Path.Combine(bin, "npm"), """
            #!/bin/bash
            if [ "$1" = pack ]; then
                printf '[{"filename":"fixture.tgz"}]\n'
            fi
            """);
        await File.WriteAllTextAsync(Path.Combine(bin, "node"), """
            #!/bin/bash
            case "$1" in
                -e) printf 'fixture.tgz' ;;
                */verify.mjs) exit 42 ;;
            esac
            """);
        await File.WriteAllTextAsync(Path.Combine(bin, "npx"), "#!/bin/bash\nexit 0\n");
        await File.WriteAllTextAsync(Path.Combine(bin, "docker"), "#!/bin/bash\nexit 1\n");

        var chmod = await ProcessRunner.RunAsync(output, "chmod",
            ["+x", .. Directory.GetFiles(bin)], workspace.Path);
        Assert.Equal(0, chmod.ExitCode);

        var result = await ProcessRunner.RunAsync(output, "bash",
            [Path.Combine(RepoRoot.Path, ".github", "workflows", "polyglot-validation", "test-nuxt-integration.sh"), repository],
            workspace.Path,
            new Dictionary<string, string>
            {
                ["PATH"] = bin + Path.PathSeparator + Environment.GetEnvironmentVariable("PATH"),
                ["HOME"] = home,
                ["NUXT_DIAGNOSTICS_DIR"] = diagnostics,
                ["TEST_DESCRIBE_SUCCEEDS"] = describeSucceeds ? "true" : "false",
                ["TEST_DESCRIBE_JSON"] = describeJson,
            });

        Assert.Equal(42, result.ExitCode);
        var childLogs = Path.Combine(diagnostics, "apphost-logs");
        Assert.Equal("startup evidence\nhttp://localhost:1234/login?t=<redacted>\n",
            await File.ReadAllTextAsync(Path.Combine(childLogs, "cli_new_detach-child_new.log")));

        if (describeSucceeds)
        {
            await Verifier.Verify(await File.ReadAllTextAsync(Path.Combine(diagnostics, "resource-snapshot.json")), "json")
                .UseDirectory("Snapshots")
                .UseParameters(describeSucceeds);
        }
    }
}
