#!/usr/bin/env bash
# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
cd "$repo_root"
rid="${1:-linux-x64}"
case "$rid" in
    linux-x64|linux-arm64|osx-x64|osx-arm64) ;;
    *) echo "Unsupported native hosting RID: $rid" >&2; exit 1 ;;
esac
if [[ "$(node -p 'Number(process.versions.node.split(".")[0])')" -lt 24 ]]; then
    echo "Native hosting validation requires Node.js 24 or later." >&2
    exit 1
fi
docker info >/dev/null
if [[ "$rid" == osx-* && "$(xcode-select -p)" == */CommandLineTools ]]; then
    # NativeAOT otherwise probes xcodebuild to choose a linker. Command Line Tools
    # provide clang and the SDK but not xcodebuild; select their modern linker explicitly.
    export UseLdClassicXCodeLinker=false
fi

# All preparation is confined to this lane's artifacts. Never borrow an existing
# published server, SDK or AppHost fixture: stale local artifacts can hide regressions.
work="$repo_root/artifacts/native-hosting/ci"
rm -rf "$work"
mkdir -p "$work/logs" "$work/scratch" "$work/runtime-sdk" "$work/cli-apphost/.aspire/modules"
export TMPDIR="$work/scratch"
export PLAYWRIGHT_BROWSERS_PATH="$work/browser"
export MSBUILDTERMINALLOGGER=false
export DOTNET_NOLOGO=1
export DOTNET_ROOT="$repo_root/.dotnet"
export PATH="$repo_root/.dotnet:$PATH"

run_logged() {
    local name="$1"
    shift
    echo "=== $name ==="
    "$@" 2>&1 | tee "$work/logs/$name.log"
}

run_logged restore ./restore.sh
run_logged devtunnel-parser-tests node --test playground/NativeHosting/devtunnel-output.test.mts

# The native core, server, build-time generator and AOT smoke host target net11.
# Keep the published executable on the same runtime as its regression suite.
run_logged cli-build dotnet build src/Aspire.Cli/Aspire.Cli.csproj -c Debug
run_logged dashboard-build dotnet build src/Aspire.Dashboard/Aspire.Dashboard.csproj -c Debug
run_logged dcp-build dotnet build "eng/dcppack/Aspire.Hosting.Orchestration.$rid.csproj" -c Debug
run_logged server-publish dotnet publish src/Aspire.Hosting.Native.Server/Aspire.Hosting.Native.Server.csproj \
    -c Release -r "$rid" -o "$work/server"
run_logged smoke-publish dotnet publish tests/Aspire.Hosting.Native.Core.Tests/AotSmoke/Aspire.Hosting.Native.Core.AotSmoke.csproj \
    -c Release -r "$rid" -o "$work/smoke"
run_logged aot-smoke "$work/smoke/Aspire.Hosting.Native.Core.AotSmoke"

cli_dll="$(dotnet msbuild src/Aspire.Cli/Aspire.Cli.csproj -nologo -getProperty:TargetPath)"
cli="$(dirname "$cli_dll")/aspire"
export ASPIRE_NATIVE_DASHBOARD_DLL
ASPIRE_NATIVE_DASHBOARD_DLL="$(dotnet msbuild src/Aspire.Dashboard/Aspire.Dashboard.csproj -nologo -getProperty:TargetPath)"
dcp_intermediate="$(dotnet msbuild "eng/dcppack/Aspire.Hosting.Orchestration.$rid.csproj" -nologo -getProperty:IntermediateOutputPath)"
export ASPIRE_NATIVE_DCP_PATH="$dcp_intermediate/tools/dcp"
test -x "$cli"
test -x "$ASPIRE_NATIVE_DCP_PATH"
test -f "$ASPIRE_NATIVE_DASHBOARD_DLL"

sdk="$work/runtime-sdk"
fixtures="$repo_root/tests/Aspire.Hosting.Native.Core.Tests/AotSmoke"
generated="$repo_root/artifacts/obj/Aspire.Hosting.Native.Server/Release/native-ats"
cp "$generated/"*.mts "$sdk/"
cp "$generated/contract.json" "$sdk/"
cp "$fixtures/"*.mts "$sdk/"
# Install only the smoke dependencies before copying the template manifest, so its
# unrelated development tools are not installed. No repository manifest/lock is edited.
run_logged npm-install npm install --prefix "$sdk" --no-save --package-lock=false \
    typescript@5.9.3 @types/node@24 vscode-jsonrpc@8.2.1 pg@8.16.3 @types/pg@8.15.5 playwright@1.58.2
cp src/Aspire.Hosting.CodeGeneration.TypeScript/Resources/package.json "$sdk/package.json"
browser_args=(install chromium)
if [[ "$rid" == linux-* ]]; then
    browser_args=(install --with-deps chromium)
fi
run_logged browser-install "$sdk/node_modules/.bin/playwright" "${browser_args[@]}"
run_logged sdk-compile "$sdk/node_modules/.bin/tsc" --module NodeNext --moduleResolution NodeNext \
    --target ES2022 --strict --skipLibCheck --esModuleInterop --outDir "$sdk/out" "$sdk/"*.mts

workspace="$work/cli-apphost"
cp "$fixtures/CliAppHost/"* "$workspace/"
cp tests/PolyglotAppHosts/Aspire.Hosting/TypeScript/package.json "$workspace/package.json"
cp src/Aspire.Cli/Templating/Templates/ts-starter/tsconfig.apphost.json "$workspace/"
cp "$generated/"*.mts "$workspace/.aspire/modules/"
# CLI's real npm install owns its node_modules. Resolve the integration worker from
# the SDK workspace so that install cannot prune its pg/playwright smoke dependencies.
ln -s "$sdk/out" "$workspace/out"
server="$work/server/Aspire.Hosting.Native.Server"

# The RPC fixture exercises AotSmoke's line-delimited --rpc host, while the runtime
# fixture below exercises the production server's authenticated socket transport.
run_logged rpc-smoke env -u ASPIRE_NATIVE_DCP_PATH node "$sdk/out/NativeRpcSmoke.mjs" \
    "$work/smoke/Aspire.Hosting.Native.Core.AotSmoke"
run_logged runtime-smoke env -u ASPIRE_NATIVE_DCP_PATH node "$sdk/out/NativeRuntimeSmoke.mjs" "$server"
run_logged baseline-smoke node "$sdk/out/NativeBaselineSmoke.mjs" "$server"
run_logged workload-dashboard-smoke node "$sdk/out/NativeWorkloadSmoke.mjs" "$server"
run_logged revision-dashboard-smoke node "$sdk/out/NativeRevisionSmoke.mjs" "$server"
run_logged cli-dashboard-smoke node "$sdk/out/NativeCliSmoke.mjs" "$cli" "$server" "$workspace"
# The auxiliary fixture responds to confirmations itself; keep the real Dashboard
# running via the CLI but do not ask a browser to handle this scenario's confirmation.
run_logged auxiliary-smoke env -u ASPIRE_NATIVE_DASHBOARD_DLL \
    ASPIRE_DASHBOARD_PATH="$ASPIRE_NATIVE_DASHBOARD_DLL" \
    node "$sdk/out/NativeAuxiliarySmoke.mjs" "$cli" "$server" "$workspace"
echo "Native hosting validation passed, including the suites' owned-resource cleanup assertions."
