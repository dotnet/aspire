#!/bin/bash
set -euo pipefail

REPOSITORY_ROOT="${1:?Repository root is required}"
WORK_ROOT="$(mktemp -d)"
TEST_NAME="aspire-nuxt-$(basename "$WORK_ROOT" | tr '[:upper:]' '[:lower:]')"
APPHOST="$WORK_ROOT/playground/NuxtApp/apphost.mts"
START_ATTEMPTED=false
API_IMAGE="$TEST_NAME-api"
WEB_IMAGE="$TEST_NAME-web"
DIAGNOSTICS_DIR="${NUXT_DIAGNOSTICS_DIR:-}"
DETACHED_LOG_BASELINE=""

collect_diagnostics() {
    if [ -z "$DIAGNOSTICS_DIR" ]; then
        return
    fi

    mkdir -p "$DIAGNOSTICS_DIR"

    # The validation container installs Aspire under $HOME/.aspire. Read the child
    # log directly so startup failures do not require a working backchannel.
    if [ -n "$DETACHED_LOG_BASELINE" ]; then
        for log in "$HOME/.aspire/logs"/cli_*_detach-child_*.log; do
            [ -f "$log" ] || continue
            if ! grep -Fxq -- "$log" "$DETACHED_LOG_BASELINE"; then
                mkdir -p "$DIAGNOSTICS_DIR/apphost-logs"
                sed -E 's#(/login\?t=)[^[:space:]"]+#\1<redacted>#g' "$log" \
                    > "$DIAGNOSTICS_DIR/apphost-logs/$(basename "$log")"
            fi
        done
    fi

    {
        printf 'node: '
        node --version
        printf 'npm: '
        npm --version
        printf 'aspire: '
        aspire --version
    } > "$DIAGNOSTICS_DIR/versions.txt" 2>&1 || true

    for directory in "$WORK_ROOT/playground/NuxtApp" "$WORK_ROOT/playground/NuxtApp/api" "$WORK_ROOT/playground/NuxtApp/web"; do
        if [ -f "$directory/package.json" ]; then
            package_name="$(basename "$directory")"
            cp "$directory/package.json" "$DIAGNOSTICS_DIR/$package_name-package.json"
            [ ! -f "$directory/package-lock.json" ] || cp "$directory/package-lock.json" "$DIAGNOSTICS_DIR/$package_name-package-lock.json"
            (cd "$directory" && npm ls --all --json) > "$DIAGNOSTICS_DIR/$package_name-dependencies.json" 2>&1 || true
        fi
    done

    if [ -d "$WORK_ROOT/publish" ]; then
        cp -R "$WORK_ROOT/publish" "$DIAGNOSTICS_DIR/publish"
    fi

    if [ "$START_ATTEMPTED" = true ]; then
        resource_snapshot="$DIAGNOSTICS_DIR/resource-snapshot.json"
        resource_snapshot_full="$WORK_ROOT/resource-snapshot.full.json"
        if aspire describe --apphost "$APPHOST" --format Json --nologo > "$resource_snapshot_full" 2> "$DIAGNOSTICS_DIR/describe-error.txt"; then
            # Environment, properties, commands, and dashboard URLs can carry credentials. Keep only
            # lifecycle and health fields needed to explain why the fixture failed to start.
            jq '{ resources: [.resources[] | {
                name,
                displayName,
                resourceType,
                state,
                exitCode,
                healthStatus,
                source
            }] }' "$resource_snapshot_full" > "$resource_snapshot"
            rm "$DIAGNOSTICS_DIR/describe-error.txt"
        fi

        for resource in api web api-installer web-installer; do
            aspire logs "$resource" --apphost "$APPHOST" --tail 500 --timestamps --nologo 2>&1 \
                | sed -E 's#(/login\?t=)[^[:space:]"]+#\1<redacted>#g' \
                > "$DIAGNOSTICS_DIR/$resource.log" || true
        done
    fi

    for name in "$TEST_NAME-web" "$TEST_NAME-api"; do
        if docker container inspect "$name" > /dev/null 2>&1; then
            docker logs "$name" > "$DIAGNOSTICS_DIR/$name.log" 2>&1 || true
        fi
    done
}

cleanup() {
    local result=$?
    if [ "$result" -ne 0 ]; then
        collect_diagnostics || true
    fi
    if [ "$START_ATTEMPTED" = true ]; then
        aspire stop --apphost "$APPHOST" --non-interactive --nologo || { [ "$result" -ne 0 ] || result=1; }
    fi
    for name in "$TEST_NAME-web" "$TEST_NAME-api"; do
        if docker container inspect "$name" > /dev/null 2>&1; then
            if [ "$result" -ne 0 ]; then
                docker logs "$name" || { [ "$result" -ne 0 ] || result=1; }
            fi
            docker rm --force "$name" > /dev/null || { [ "$result" -ne 0 ] || result=1; }
        fi
    done
    if docker network inspect "$TEST_NAME" > /dev/null 2>&1; then
        docker network rm "$TEST_NAME" > /dev/null || { [ "$result" -ne 0 ] || result=1; }
    fi
    for image in "$WEB_IMAGE" "$API_IMAGE"; do
        if docker image inspect "$image" > /dev/null 2>&1; then
            docker image rm "$image" > /dev/null || { [ "$result" -ne 0 ] || result=1; }
        fi
    done
    rm -rf -- "$WORK_ROOT"
    exit "$result"
}
trap cleanup EXIT

# Keep SDKs, npm restore changes, and Nuxt output out of the source checkout.
# Preserve the package/AppHost relative layout for the package-authoring restore.
tar -C "$REPOSITORY_ROOT" \
    --exclude=node_modules --exclude=.aspire --exclude=dist --exclude=.nuxt --exclude=.output \
    -cf - src/Aspire.Hosting.Nuxt playground/NuxtApp | tar -C "$WORK_ROOT" -xf -

echo "=== Build and pack the TypeScript-authored Nuxt integration ==="
(
    cd "$WORK_ROOT/src/Aspire.Hosting.Nuxt"
    aspire restore --non-interactive --nologo
    npm pack --pack-destination "$WORK_ROOT" --json > "$WORK_ROOT/pack.json"
)
PACKAGE_FILE="$(node -e 'process.stdout.write(JSON.parse(require("node:fs").readFileSync(process.argv[1], "utf8"))[0].filename)' "$WORK_ROOT/pack.json")"
(
    cd "$WORK_ROOT/playground/NuxtApp"
    npm install --no-audit --no-fund "$WORK_ROOT/$PACKAGE_FILE"
    node --input-type=module -e '
        import fs from "node:fs";
        const config = JSON.parse(fs.readFileSync("aspire.config.json", "utf8"));
        config.packages["@aspire/nuxt"].path = "./node_modules/@aspire/nuxt/dist/src/host.js";
        fs.writeFileSync("aspire.config.json", JSON.stringify(config, null, 2));
    '
    aspire restore --apphost "$APPHOST" --non-interactive --nologo
    npx --no-install tsc --noEmit --project tsconfig.apphost.json
)

echo "=== Run the installed preview package and verify real Nuxt SSR ==="
# Exclude child logs from earlier validation fixtures, including when this
# invocation fails before it can report its log path.
DETACHED_LOG_BASELINE="$WORK_ROOT/detached-logs.before"
: > "$DETACHED_LOG_BASELINE"
for log in "$HOME/.aspire/logs"/cli_*_detach-child_*.log; do
    [ ! -f "$log" ] || printf '%s\n' "$log" >> "$DETACHED_LOG_BASELINE"
done
START_ATTEMPTED=true
aspire start --apphost "$APPHOST" --isolated --non-interactive --nologo
node "$WORK_ROOT/playground/NuxtApp/verify.mjs" apphost "$APPHOST"
aspire stop --apphost "$APPHOST" --non-interactive --nologo
START_ATTEMPTED=false

echo "=== Publish and run the generated production images ==="
aspire publish --apphost "$APPHOST" --output-path "$WORK_ROOT/publish" --non-interactive --nologo
docker build --tag "$API_IMAGE" --file "$WORK_ROOT/publish/api.Dockerfile" "$WORK_ROOT/playground/NuxtApp/api"
docker build --tag "$WEB_IMAGE" --file "$WORK_ROOT/publish/web.Dockerfile" "$WORK_ROOT/playground/NuxtApp/web"
docker network create "$TEST_NAME" > /dev/null
docker run --detach --name "$TEST_NAME-api" --network "$TEST_NAME" --network-alias api \
    --env PORT=8000 --env HOST=0.0.0.0 "$API_IMAGE" > /dev/null
docker cp "$WORK_ROOT/playground/NuxtApp/verify.mjs" "$TEST_NAME-api:/tmp/verify.mjs"
docker exec "$TEST_NAME-api" node /tmp/verify.mjs health http://127.0.0.1:8000/health
# Read the published reference rather than supplying a separate test-only URL.
# Compose emits a quoted scalar, for example: NUXT_API_BASE: "http://api:8000".
NUXT_API_BASE="$(node -e '
    const text = require("node:fs").readFileSync(process.argv[1], "utf8");
    const value = text.match(/^\s+NUXT_API_BASE: "([^"]+)"$/m)?.[1];
    if (value !== "http://api:8000") throw new Error(`Unexpected published backend reference: ${value}`);
    process.stdout.write(value);
' "$WORK_ROOT/publish/docker-compose.yaml")"
docker run --detach --name "$TEST_NAME-web" --network "$TEST_NAME" \
    --env PORT=8001 --env HOST=0.0.0.0 --env NUXT_API_BASE="$NUXT_API_BASE" "$WEB_IMAGE" > /dev/null
# The CI validator talks to the host's Docker daemon through its mounted socket.
# Host-published loopback ports are not the validator's loopback; probe in the image.
docker cp "$WORK_ROOT/playground/NuxtApp/verify.mjs" "$TEST_NAME-web:/tmp/verify.mjs"
docker exec "$TEST_NAME-web" node /tmp/verify.mjs url http://127.0.0.1:8001
echo "=== Packed Nuxt integration validated in run and publish modes ==="
