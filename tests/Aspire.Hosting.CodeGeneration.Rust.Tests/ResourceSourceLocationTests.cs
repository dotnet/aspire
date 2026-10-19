// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Aspire.TestUtilities;
using Aspire.TypeSystem;

namespace Aspire.Hosting.CodeGeneration.Rust.Tests;

public class ResourceSourceLocationTests(ITestOutputHelper outputHelper)
{
    [Theory]
    [InlineData("ascii", "ASCII")]
    [InlineData("tab", "\t")]
    [InlineData("emoji", "\U0001F600")]
    [InlineData("wide", "\u4E2D")]
    [InlineData("combining", "e\u0301")]
    [RequiresTools(["cargo"])]
    public async Task GeneratedTransportCapturesExecutedCallsAndRejectsNewerSource(string caseName, string prefix)
    {
        using var workspace = TemporaryWorkspace.Create(outputHelper);
        var files = new AtsRustCodeGenerator().GenerateDistributedApplication(new AtsContext
        {
            Capabilities = [],
            HandleTypes = [],
            EnumTypes = [],
            DtoTypes = []
        });
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "transport.rs"), files["transport.rs"]);
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "Cargo.toml"), """
            [package]
            name = "resource-source-probe"
            version = "0.0.0"
            edition = "2021"

            [[bin]]
            name = "resource-source-probe"
            path = "main.rs"

            [dependencies]
            serde = { version = "1.0", features = ["derive"] }
            serde_json = "1.0"
            lazy_static = "1.4"
            """);
        await File.WriteAllTextAsync(Path.Combine(workspace.Path, "main.rs"),
            Probe.Replace("SOURCE_UNICODE", prefix, StringComparison.Ordinal).ReplaceLineEndings("\n"));

        var startInfo = new ProcessStartInfo("cargo")
        {
            WorkingDirectory = workspace.Path,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        startInfo.ArgumentList.Add("run");
        startInfo.ArgumentList.Add("--quiet");
        startInfo.Environment["ASPIRE_PROJECT_DIRECTORY"] = workspace.Path;
        using var process = Process.Start(startInfo)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(3));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            await process.WaitForExitAsync();
            throw;
        }

        Assert.True(process.ExitCode == 0, await stderr);
        await Verify(await stdout).UseParameters(caseName);
    }

    private const string Probe = """
        #[allow(dead_code, unused_imports, unused_variables)]
        mod transport;

        #[track_caller]
        fn add_resource() -> serde_json::Value {
            transport::resource_source_location("add_resource").expect("Missing source")
        }

        fn main() {
            let compiled_source = include_str!("main.rs");
            for condition in [true, false] {
                let _prefix = "SOURCE_UNICODE"; let source = if condition { add_resource() } else { add_resource() };
                let (line_index, line) = compiled_source.lines().enumerate()
                    .find(|(_, text)| text.contains("let _prefix =")).unwrap();
                let offset = if condition { line.find("add_resource()") } else { line.rfind("add_resource()") }.unwrap();
                assert_eq!(source["lineNumber"], line_index + 1);
                if line.is_ascii() {
                    assert_eq!(source["columnNumber"], line[..offset].encode_utf16().count() + 1);
                } else {
                    assert_eq!(source["columnNumber"], serde_json::Value::Null);
                }
                assert_eq!(source["methodName"], "add_resource");
                let path = std::path::PathBuf::from(source["filePath"].as_str().unwrap());
                assert!(path.is_absolute());
                assert_eq!(std::fs::canonicalize(path).unwrap(), std::fs::canonicalize("main.rs").unwrap());
                println!("{}:{}:{}:{}", source["lineNumber"], source["columnNumber"],
                    source["methodName"].as_str().unwrap(), source["documentHash"].as_str().unwrap());
            }
            std::fs::write("main.rs", format!("{compiled_source}\n// Edited after compilation.")).unwrap();
            std::fs::OpenOptions::new().write(true).open("main.rs").unwrap()
                .set_modified(std::time::SystemTime::now() + std::time::Duration::from_secs(1)).unwrap();
            assert!(transport::resource_source_location("add_resource").is_none());
            println!("Stale source suppressed");
        }
        """;
}
