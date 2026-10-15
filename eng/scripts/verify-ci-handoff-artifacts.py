import argparse
import filecmp
import json
import os
from pathlib import Path
import subprocess
import tarfile
import zipfile


def single(root, pattern):
    paths = list(root.rglob(pattern))
    if len(paths) != 1:
        raise ValueError(f"Expected exactly one {pattern} in {root}, found {paths}")
    return paths[0]


def unpack(archive, destination):
    destination.mkdir(parents=True)
    if archive.name.endswith(".zip") or archive.suffix == ".nupkg":
        with zipfile.ZipFile(archive) as package:
            package.extractall(destination)
    else:
        with tarfile.open(archive, "r:gz") as package:
            package.extractall(destination, filter="data")


def assert_identical(expected, actual, label):
    if not expected.is_dir() or not actual.is_dir():
        raise ValueError(f"{label}: comparison directory is missing")
    left = {p.relative_to(expected).as_posix(): p for p in expected.rglob("*") if p.is_file()}
    right = {p.relative_to(actual).as_posix(): p for p in actual.rglob("*") if p.is_file()}
    if not left or left.keys() != right.keys():
        raise AssertionError(
            f"{label}: file set differs; missing={sorted(left.keys() - right.keys())}, "
            f"extra={sorted(right.keys() - left.keys())}"
        )
    changed = [name for name in left if not filecmp.cmp(left[name], right[name], shallow=False)]
    if changed:
        raise AssertionError(f"{label}: file bytes differ: {changed}")
    print(f"{label}: all {len(left)} files byte-identical", flush=True)
    return {"comparison": label, "files": len(left), "result": "byte-identical"}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--rid", required=True, choices=["linux-x64", "win-x64", "osx-arm64"])
    parser.add_argument("--source-sha", required=True)
    args = parser.parse_args()
    inputs = Path("artifacts/handoff-input").resolve()
    evidence = Path("artifacts/handoff-evidence").resolve()
    evidence.mkdir(parents=True)
    rid = args.rid
    windows = rid.startswith("win-")
    suffix = ".exe" if windows else ""
    archive_suffix = "zip" if windows else "tar.gz"
    unpack(single(inputs / "packages", f"Aspire.Dashboard.Sdk.{rid}.*.nupkg"), evidence / "package")
    unpack(single(inputs / "dashboard", f"aspire-dashboard-{rid}.zip"), evidence / "dashboard")
    unpack(single(inputs / "payload", f"*{rid}.tar.gz"), evidence / "payload")
    unpack(single(inputs / "cli", f"aspire-cli-{rid}-*.{archive_suffix}"), evidence / "cli")
    cli = evidence / "cli" / f"aspire{suffix}"
    installed = evidence / "installed"
    environment = os.environ | {"ASPIRE_LAYOUT_PATH": str(installed)}
    subprocess.run(
        [str(cli), "setup", "--install-path", str(installed), "--force"],
        env=environment, check=True
    )
    version_result = subprocess.run(
        [str(cli), "--version"], env=environment, text=True, stdout=subprocess.PIPE
    )
    print(version_result.stdout, end="", flush=True)
    version_result.check_returncode()
    version = version_result.stdout.strip()
    if args.source_sha[:8] not in version:
        raise AssertionError(f"Native CLI version {version!r} does not identify {args.source_sha}")
    bundle = installed / "bundle"
    checks = [
        assert_identical(evidence / "package" / "tools", evidence / "dashboard",
                         "Dashboard NuGet / standalone ZIP"),
        assert_identical(evidence / "package" / "tools", bundle / "dashboard",
                         "Dashboard NuGet / actual CLI extraction"),
        assert_identical(evidence / "payload" / rid, bundle,
                         "Transferred prepared payload / actual CLI extraction"),
    ]
    (evidence / "comparison.json").write_text(
        json.dumps({"rid": rid, "source_sha": args.source_sha, "cli_version": version,
                    "checks": checks}, indent=2) + "\n", encoding="utf-8"
    )
    print(f"HANDOFF_ARTIFACTS_VERIFIED rid={rid} sha={args.source_sha}", flush=True)


if __name__ == "__main__":
    main()
