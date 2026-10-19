#!/usr/bin/env python3
"""Run the prototype across two independently initialized Git repositories."""

import argparse
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import time
import urllib.error
import urllib.request


HERE = Path(__file__).resolve().parent
ASPIRE = HERE.parent.parent


def run(command, *, cwd=ASPIRE, env=None):
    print("+", " ".join(str(value) for value in command), flush=True)
    subprocess.run(command, cwd=cwd, env=env, check=True)


def build(project, env):
    dotnet = str(ASPIRE / "dotnet.sh")
    run([dotnet, "restore", str(project), "--disable-parallel",
         "--configfile", str(ASPIRE / "NuGet.config"), "-p:NuGetAudit=false", "-v:q"], env=env)
    run([dotnet, "build", str(project), "--no-restore", "-v:q"], env=env)


def prepare(workspace):
    workspace.mkdir(parents=True, exist_ok=False)
    for name in ("producer", "consumer"):
        target = workspace / name
        shutil.copytree(HERE / name, target,
                        ignore=shutil.ignore_patterns("bin", "obj", ".git", "publish", ".aspire"))
        run(["git", "init", "--quiet"], cwd=target)
        if name == "consumer":
            contracts = target / "contracts"
            contracts.mkdir()
            shutil.copy2(workspace / "producer/aspire-service.json", contracts / "catalog.json")
        run(["git", "add", "."], cwd=target)
        run(["git", "-c", "user.name=Contract fixture", "-c", "user.email=fixture@example.invalid",
             "commit", "--quiet", "-m",
             "Initialize independently owned fixture\n\nCo-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>"],
            cwd=target)


def free_port():
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


def request_through_consumer(workspace, runtime, dcp, dashboard, mode, source=None, missing_key=False):
    port = free_port()
    apphost = workspace / "consumer/AppHost"
    arguments = [
        runtime, str(apphost / "bin/Debug/net10.0/Consumer.AppHost.dll"),
        "--DcpPublisher:CliPath", str(dcp), "--DcpPublisher:DashboardPath", str(dashboard),
        "--StorefrontPort", str(port),
        "--Parameters:catalog-region", "north",
    ]
    if not missing_key:
        arguments += ["--Parameters:catalog-api-key", "fixture-key-not-a-secret"]
    if source is not None:
        arguments += ["--CatalogSource", str(source)]
    log_path = workspace / f"{mode}.log"
    with log_path.open("w") as log:
        process = subprocess.Popen(arguments, cwd=apphost, stdout=log, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + 120
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    raise RuntimeError(f"{mode}: AppHost exited {process.returncode}; see {log_path}")
                if missing_key:
                    output = log_path.read_text()
                    if "catalog-api-key" in output and (
                            "could not be initialized" in output or
                            "Failed to initialize parameter resource" in output):
                        try:
                            urllib.request.urlopen(f"http://127.0.0.1:{port}/shop", timeout=2).close()
                        except (urllib.error.URLError, TimeoutError):
                            pass
                        else:
                            raise AssertionError("Consumer responded successfully without the required API key")
                        print(f"PASS {mode}: missing parameter reported; dependent service cannot start", flush=True)
                        return
                try:
                    with urllib.request.urlopen(f"http://127.0.0.1:{port}/shop", timeout=2) as response:
                        payload = json.load(response)
                    assert not missing_key, "Consumer unexpectedly started without the required API key"
                    assert payload == {
                        "service": "catalog", "contractVersion": 1, "region": "north",
                        "products": ["tea", "coffee"],
                    }, payload
                    print(f"PASS {mode}: consumer /shop -> producer /products: {json.dumps(payload)}", flush=True)
                    return payload
                except (urllib.error.URLError, TimeoutError):
                    time.sleep(0.5)
            raise TimeoutError(f"{mode}: no successful request within 120 seconds; see {log_path}")
        finally:
            process.terminate()
            try:
                process.wait(timeout=30)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait(timeout=10)


def rejects(workspace, runtime, name, expected, source=None):
    apphost = workspace / "consumer/AppHost"
    arguments = [runtime, str(apphost / "bin/Debug/net10.0/Consumer.AppHost.dll"),
                 "--Parameters:catalog-region", "north",
                 "--Parameters:catalog-api-key", "fixture-key-not-a-secret"]
    if source is not None:
        arguments += ["--CatalogSource", str(source)]
    result = subprocess.run(arguments, cwd=apphost, capture_output=True, text=True, timeout=30)
    output = result.stdout + result.stderr
    (workspace / f"{name}.log").write_text(output)
    assert result.returncode != 0 and expected in output, (name, result.returncode, output)
    print(f"PASS {name}: {expected}", flush=True)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--workspace", required=True, type=Path, help="New directory; never overwrites an existing workspace")
    parser.add_argument("--prepare-only", action="store_true")
    parser.add_argument("--mode", choices=("all", "source", "container"), default="all")
    args = parser.parse_args()
    workspace = args.workspace.resolve()
    prepare(workspace)
    if args.prepare_only:
        print(f"Independent repositories ready in {workspace}")
        return

    env = os.environ | {"AspireRepoRoot": str(ASPIRE)}
    dotnet = str(ASPIRE / "dotnet.sh")
    runtime = shutil.which("dotnet")
    if runtime is None:
        raise RuntimeError("Install .NET 10 and put dotnet on PATH.")
    support = ASPIRE / "src/Aspire.Hosting.AppHost/Aspire.Hosting.AppHost.csproj"
    run([dotnet, "restore", str(support), "--disable-parallel", "-p:NuGetAudit=false", "-v:q"])
    dashboard_project = ASPIRE / "src/Aspire.Dashboard/Aspire.Dashboard.csproj"
    run([dotnet, "restore", str(dashboard_project), "--disable-parallel", "-p:NuGetAudit=false", "-v:q"])
    run([dotnet, "build", str(dashboard_project), "--no-restore", "-v:q"])
    dashboard = ASPIRE / "artifacts/bin/Aspire.Dashboard/Debug/net11.0/Aspire.Dashboard.dll"
    if not dashboard.is_file():
        raise RuntimeError(f"Dashboard build did not produce {dashboard}")
    properties = json.loads(subprocess.check_output(
        [dotnet, "msbuild", str(support), "-nologo",
         "-getProperty:NuGetPackageRoot,MicrosoftDeveloperControlPlanedarwinamd64Version"], text=True))
    version = properties["Properties"]["MicrosoftDeveloperControlPlanedarwinamd64Version"]
    package_root = Path(properties["Properties"]["NuGetPackageRoot"])
    # This Linux proof of concept uses the same orchestrator version as the repository.
    dcp = package_root / f"microsoft.developercontrolplane.linux-amd64/{version}/tools/dcp"
    if not dcp.is_file():
        raise RuntimeError(f"Linux DCP was not restored at {dcp}")

    apphost_project = workspace / "consumer/AppHost/Consumer.AppHost.csproj"
    storefront = workspace / "consumer/Storefront/Storefront.csproj"
    producer = workspace / "producer"
    producer_contract_path = producer / "aspire-service.json"
    artifact_path = workspace / "consumer/contracts/catalog.json"
    original_text = artifact_path.read_text()
    original_contract = json.loads(original_text)
    for project in (apphost_project, storefront):
        build(project, env)

    if args.mode in ("all", "source"):
        project = producer / original_contract["project"]
        build(project, env)
        request_through_consumer(workspace, runtime, dcp, dashboard, "source", producer)
        request_through_consumer(workspace, runtime, dcp, dashboard, "source-missing-parameter", producer, missing_key=True)
        # Producer-only internal refactor plus a compatible minor contract release.
        relocated = producer / "src/Catalog.Renamed"
        project.parent.rename(relocated)
        evolved = original_contract | {"version": "1.1.0", "project": "src/Catalog.Renamed/Catalog.Api.csproj"}
        producer_contract_path.write_text(json.dumps(evolved, indent=2) + "\n")
        build(producer / evolved["project"], env)
        request_through_consumer(workspace, runtime, dcp, dashboard, "source-evolved", producer)
        producer_contract_path.write_text(json.dumps(evolved | {"version": "2.0.0"}) + "\n")
        rejects(workspace, runtime, "source-major-mismatch", "expected 'contoso.catalog' major 1", producer)
        relocated.rename(project.parent)
        producer_contract_path.write_text(original_text)

    for name, changed, expected in (
        ("artifact-identity-mismatch", original_contract | {"id": "contoso.other"}, "expected 'contoso.catalog' major 1"),
        ("artifact-schema-mismatch", original_contract | {"schemaVersion": 2}, "schemaVersion must be 1"),
        ("missing-binding", original_contract | {"inputs": original_contract["inputs"] + [
            {"name": "tenant", "environmentVariable": "CATALOG_TENANT"}]}, "Missing: [tenant]"),
        ("unexpected-binding", original_contract | {"inputs": original_contract["inputs"][:1]}, "Unexpected: [api-key]"),
        ("invalid-json", None, "Invalid service contract"),
    ):
        artifact_path.write_text(json.dumps(changed) if changed is not None else "{")
        rejects(workspace, runtime, name, expected)
    artifact_path.unlink()
    rejects(workspace, runtime, "missing-contract", "Obtain the producer's contract artifact")
    artifact_path.write_text(original_text)
    rejects(workspace, runtime, "missing-checkout", "select a checkout containing aspire-service.json", workspace / "absent")

    if args.mode in ("all", "container"):
        # Reuse the daemon; never start or stop a shared daemon from this script.
        run(["docker", "info", "--format", "{{.ServerVersion}}"], cwd=workspace)
        run(["docker", "build", "--quiet", "-t", "aspire-contract-catalog:1.0.0", str(producer)], cwd=workspace)
        offline = workspace / "producer-offline"
        producer.rename(offline)
        try:
            request_through_consumer(workspace, runtime, dcp, dashboard, "container-without-source")
            request_through_consumer(workspace, runtime, dcp, dashboard, "container-missing-parameter", missing_key=True)
        finally:
            offline.rename(producer)
    for repository in (producer, workspace / "consumer"):
        status = subprocess.check_output(["git", "status", "--porcelain"], cwd=repository, text=True)
        assert not status, f"Verification left tracked fixture changes in {repository}: {status}"
    print(f"PASS: evidence and independently owned repositories retained in {workspace}", flush=True)


if __name__ == "__main__":
    main()
