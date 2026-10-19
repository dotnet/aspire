#!/usr/bin/env bash
set -euo pipefail

sample="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
apphost_pid=""
stop_apphost() {
    if [[ -n "$apphost_pid" ]]; then
        local status=0
        if kill -0 "$apphost_pid" 2>/dev/null; then
            kill -TERM "$apphost_pid"
        fi
        wait "$apphost_pid" || status=$?
        apphost_pid=""
        if [[ "$status" != 0 && "$status" != 143 ]]; then
            echo "AppHost exited with status $status; inspect imported.log/replacement.log." >&2
            return "$status"
        fi
        # DCP can finish asynchronous cleanup after the AppHost has exited.
        for attempt in $(seq 1 60); do
            if ! docker ps --format '{{.Names}}' | grep -Eq '^legacy-(api|gateway|redis)-'; then
                return
            fi
            sleep 1
        done
        echo "Timed out waiting for DCP to clean up the imported containers." >&2
        return 1
    fi
}
cleanup() {
    stop_apphost
    docker compose -f "$sample/compose.yaml" down
}
trap cleanup EXIT

docker info > /dev/null
docker compose version
docker compose -f "$sample/compose.yaml" up -d --wait

check_response() {
    local expected="$1"
    local minimum="${2:-1}"
    local response=""
    for attempt in $(seq 1 90); do
        if response="$(curl -fsS --max-time 3 http://localhost:18080/)"; then
            printf '%s\n' "$response" | python3 -c '
import json, sys
body = json.load(sys.stdin)
assert body["implementation"] == sys.argv[1], body
assert body["visits"] >= int(sys.argv[2]), body
print(json.dumps(body))
' "$expected" "$minimum"
            return
        fi
        if [[ -n "$apphost_pid" ]] && ! kill -0 "$apphost_pid" 2>/dev/null; then
            echo "AppHost exited before serving requests; inspect imported.log/replacement.log." >&2
            return 1
        fi
        sleep 1
    done
    echo "Request path did not become ready for $expected" >&2
    return 1
}

echo "Original Compose stack:"
check_response compose-python
docker compose -f "$sample/compose.yaml" down

echo "Imported Aspire containers:"
bash "$sample/run.sh" > "$sample/imported.log" 2>&1 &
apphost_pid=$!
check_response compose-python
curl -fsS http://localhost:18888/ > /dev/null
stop_apphost

echo "Gateway container -> local .NET source -> Redis container:"
COMPOSE_REPLACE_API=true bash "$sample/run.sh" > "$sample/replacement.log" 2>&1 &
apphost_pid=$!
check_response local-dotnet
check_response local-dotnet 2
curl -fsS http://localhost:18888/ > /dev/null
if docker ps --format '{{.Names}}' | grep -q '^legacy-api-'; then
    echo "An API container is running despite replacement" >&2
    exit 1
fi
stop_apphost
if docker ps --format '{{.Names}}' | grep -Eq '^legacy-(api|gateway|redis)-'; then
    echo "Imported resources were not cleaned up after AppHost shutdown" >&2
    exit 1
fi
echo "All three request paths, dashboard HTTP and lifecycle cleanup verified."
