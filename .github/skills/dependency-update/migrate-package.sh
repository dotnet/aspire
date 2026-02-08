#!/usr/bin/env bash
#
# migrate-package.sh — Trigger and monitor the dotnet-migrate-package Azure DevOps pipeline
#
# Usage:
#   migrate-package.sh <PackageName> <PackageVersion> [--poll-interval <seconds>] [--timeout <seconds>]
#   migrate-package.sh --check-prereqs
#   migrate-package.sh --help
#
# Environment:
#   AZDO_ORG          Override Azure DevOps org URL (default: https://dev.azure.com/dnceng)
#   AZDO_PROJECT      Override project name (default: internal)
#   AZDO_PIPELINE_ID  Override pipeline definition ID (default: 931)

set -euo pipefail

# Defaults
AZDO_ORG="${AZDO_ORG:-https://dev.azure.com/dnceng}"
AZDO_PROJECT="${AZDO_PROJECT:-internal}"
AZDO_PIPELINE_ID="${AZDO_PIPELINE_ID:-931}"
POLL_INTERVAL=30
TIMEOUT=900  # 15 minutes

COLOR_RED='\033[0;31m'
COLOR_GREEN='\033[0;32m'
COLOR_YELLOW='\033[0;33m'
COLOR_CYAN='\033[0;36m'
COLOR_RESET='\033[0m'

usage() {
    cat <<'EOF'
migrate-package.sh — Trigger and monitor the dotnet-migrate-package Azure DevOps pipeline

USAGE:
    migrate-package.sh <PackageName> <PackageVersion> [OPTIONS]
    migrate-package.sh --check-prereqs
    migrate-package.sh --help

ARGUMENTS:
    PackageName       NuGet package ID (e.g., Hex1b)
    PackageVersion    Version to import (e.g., 0.49.0) or "latest"

OPTIONS:
    --poll-interval <seconds>   Polling interval (default: 30)
    --timeout <seconds>         Max wait time (default: 900)
    --migration-type <type>     Pipeline migration type (default: "New or non-Microsoft")
    --no-wait                   Trigger only, don't wait for completion
    --check-prereqs             Check prerequisites and exit
    --help                      Show this help

ENVIRONMENT:
    AZDO_ORG          Azure DevOps org URL (default: https://dev.azure.com/dnceng)
    AZDO_PROJECT      Project name (default: internal)
    AZDO_PIPELINE_ID  Pipeline definition ID (default: 931)

EXAMPLES:
    migrate-package.sh Hex1b 0.49.0
    migrate-package.sh StackExchange.Redis 2.9.33 --no-wait
    migrate-package.sh --check-prereqs
EOF
}

log_info()    { echo -e "${COLOR_CYAN}[INFO]${COLOR_RESET} $*"; }
log_success() { echo -e "${COLOR_GREEN}[OK]${COLOR_RESET} $*"; }
log_warn()    { echo -e "${COLOR_YELLOW}[WARN]${COLOR_RESET} $*"; }
log_error()   { echo -e "${COLOR_RED}[ERROR]${COLOR_RESET} $*" >&2; }

check_prereqs() {
    local ok=true

    # Check az CLI
    if ! command -v az &>/dev/null; then
        log_error "Azure CLI (az) is not installed."
        echo "  Install: https://learn.microsoft.com/cli/azure/install-azure-cli"
        ok=false
    else
        log_success "Azure CLI found: $(az version --query '\"azure-cli\"' -o tsv 2>/dev/null)"
    fi

    # Check devops extension
    if ! az extension show --name azure-devops &>/dev/null; then
        log_error "Azure DevOps extension is not installed."
        echo "  Install: az extension add --name azure-devops"
        ok=false
    else
        log_success "Azure DevOps extension installed"
    fi

    # Check authentication
    if ! az account show &>/dev/null 2>&1; then
        log_error "Not logged in to Azure CLI."
        echo "  Login: az login"
        ok=false
    else
        local account
        account=$(az account show --query 'user.name' -o tsv 2>/dev/null)
        log_success "Logged in as: ${account}"
    fi

    # Check org access by querying the pipeline
    if az account show &>/dev/null 2>&1; then
        if az pipelines show --id "${AZDO_PIPELINE_ID}" --org "${AZDO_ORG}" --project "${AZDO_PROJECT}" --query 'name' -o tsv &>/dev/null; then
            log_success "Pipeline access verified (dotnet-migrate-package, ID ${AZDO_PIPELINE_ID})"
        else
            log_warn "Cannot access pipeline ${AZDO_PIPELINE_ID}. You may need access to ${AZDO_ORG}/${AZDO_PROJECT}"
            ok=false
        fi
    fi

    if [ "$ok" = true ]; then
        log_success "All prerequisites met"
        return 0
    else
        log_error "Some prerequisites are missing. See above for details."
        return 1
    fi
}

trigger_pipeline() {
    local package_name="$1"
    local package_version="$2"
    local migration_type="$3"

    log_info "Triggering dotnet-migrate-package pipeline..."
    log_info "  Package:        ${package_name}"
    log_info "  Version:        ${package_version}"
    log_info "  MigrationType:  ${migration_type}"

    local run_json
    run_json=$(az pipelines run \
        --id "${AZDO_PIPELINE_ID}" \
        --org "${AZDO_ORG}" \
        --project "${AZDO_PROJECT}" \
        --parameters \
            "PackageNames=${package_name}" \
            "PackageVersion=${package_version}" \
            "MigrationType=${migration_type}" \
        -o json 2>&1)

    if [ $? -ne 0 ]; then
        log_error "Failed to trigger pipeline: ${run_json}"
        return 1
    fi

    local run_id
    run_id=$(echo "$run_json" | python3 -c "import json,sys; print(json.load(sys.stdin)['id'])")

    local run_url="${AZDO_ORG}/${AZDO_PROJECT}/_build/results?buildId=${run_id}"

    log_success "Pipeline triggered successfully"
    log_info "  Run ID: ${run_id}"
    log_info "  URL:    ${run_url}"

    echo "$run_id"
}

poll_pipeline() {
    local run_id="$1"
    local start_time
    start_time=$(date +%s)

    log_info "Polling pipeline run ${run_id} (interval: ${POLL_INTERVAL}s, timeout: ${TIMEOUT}s)..."

    while true; do
        local elapsed=$(( $(date +%s) - start_time ))

        if [ "$elapsed" -ge "$TIMEOUT" ]; then
            log_error "Timeout after ${TIMEOUT}s waiting for pipeline run ${run_id}"
            return 2
        fi

        local run_json
        run_json=$(az pipelines runs show \
            --id "$run_id" \
            --org "${AZDO_ORG}" \
            --project "${AZDO_PROJECT}" \
            -o json 2>&1)

        local status result
        status=$(echo "$run_json" | python3 -c "import json,sys; print(json.load(sys.stdin).get('status','unknown'))")
        result=$(echo "$run_json" | python3 -c "import json,sys; print(json.load(sys.stdin).get('result','') or '')")

        local mins=$((elapsed / 60))
        local secs=$((elapsed % 60))

        if [ "$status" = "completed" ]; then
            if [ "$result" = "succeeded" ]; then
                log_success "Pipeline completed successfully (${mins}m${secs}s)"
                return 0
            else
                log_error "Pipeline completed with result: ${result} (${mins}m${secs}s)"
                local run_url="${AZDO_ORG}/${AZDO_PROJECT}/_build/results?buildId=${run_id}"
                log_error "See: ${run_url}"
                return 1
            fi
        fi

        log_info "  Status: ${status} (elapsed: ${mins}m${secs}s)..."
        sleep "$POLL_INTERVAL"
    done
}

# --- Main ---

PACKAGE_NAME=""
PACKAGE_VERSION=""
MIGRATION_TYPE="New or non-Microsoft"
NO_WAIT=false
CHECK_PREREQS=false

while [ $# -gt 0 ]; do
    case "$1" in
        --help|-h)
            usage
            exit 0
            ;;
        --check-prereqs)
            CHECK_PREREQS=true
            shift
            ;;
        --poll-interval)
            POLL_INTERVAL="$2"
            shift 2
            ;;
        --timeout)
            TIMEOUT="$2"
            shift 2
            ;;
        --migration-type)
            MIGRATION_TYPE="$2"
            shift 2
            ;;
        --no-wait)
            NO_WAIT=true
            shift
            ;;
        -*)
            log_error "Unknown option: $1"
            usage
            exit 1
            ;;
        *)
            if [ -z "$PACKAGE_NAME" ]; then
                PACKAGE_NAME="$1"
            elif [ -z "$PACKAGE_VERSION" ]; then
                PACKAGE_VERSION="$1"
            else
                log_error "Unexpected argument: $1"
                usage
                exit 1
            fi
            shift
            ;;
    esac
done

if [ "$CHECK_PREREQS" = true ]; then
    check_prereqs
    exit $?
fi

if [ -z "$PACKAGE_NAME" ] || [ -z "$PACKAGE_VERSION" ]; then
    log_error "PackageName and PackageVersion are required."
    echo ""
    usage
    exit 1
fi

# Run prerequisite checks first
if ! check_prereqs; then
    exit 1
fi

echo ""

# Trigger the pipeline (capture run ID from last line of output)
RUN_OUTPUT=$(trigger_pipeline "$PACKAGE_NAME" "$PACKAGE_VERSION" "$MIGRATION_TYPE")
RUN_ID=$(echo "$RUN_OUTPUT" | tail -1)

if [ -z "$RUN_ID" ] || ! [[ "$RUN_ID" =~ ^[0-9]+$ ]]; then
    log_error "Failed to get run ID from pipeline trigger"
    exit 1
fi

if [ "$NO_WAIT" = true ]; then
    log_info "Skipping wait (--no-wait). Monitor at:"
    log_info "  ${AZDO_ORG}/${AZDO_PROJECT}/_build/results?buildId=${RUN_ID}"
    exit 0
fi

echo ""

# Poll until completion
poll_pipeline "$RUN_ID"
