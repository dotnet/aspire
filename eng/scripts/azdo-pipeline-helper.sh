#!/bin/bash

# Azure DevOps Pipeline Helper Script
# Automates triggering pipelines and monitoring results for testing matrix migration changes

set -euo pipefail

# Configuration
ORGANIZATION="dnceng-public"
PROJECT="public"
REPO_NAME="aspire"
PIPELINE_NAME="azdo-tests"  # The manual test pipeline

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Help function
show_help() {
    cat << EOF
Azure DevOps Pipeline Helper for Aspire Matrix Migration Testing

Usage: $0 [COMMAND] [OPTIONS]

Commands:
    trigger     Trigger the azdo-tests pipeline on specified branch
    status      Check status of a running build
    logs        Get logs from a build
    results     Get test results summary from a build
    setup       Setup Azure DevOps CLI and authentication
    validate    Validate current authentication and access

Options:
    --branch BRANCH         Git branch to build (required for trigger)
    --build-id BUILD_ID     Build ID to check (required for status/logs/results)
    --token TOKEN          Azure DevOps Personal Access Token
    --wait                 Wait for build completion (for trigger command)
    --follow                Follow build logs in real-time
    --help                 Show this help message

Environment Variables:
    AZDO_TOKEN             Azure DevOps Personal Access Token
    AZDO_ORG              Azure DevOps organization (default: dnceng-public)
    AZDO_PROJECT          Azure DevOps project (default: public)

Examples:
    # Setup and authenticate
    $0 setup --token YOUR_TOKEN_HERE

    # Trigger build on feature branch and wait for completion
    $0 trigger --branch feature/matrix-migration --wait

    # Check status of specific build
    $0 status --build-id 12345

    # Get logs from completed build
    $0 logs --build-id 12345

    # Get test results summary
    $0 results --build-id 12345

    # Follow logs in real-time
    $0 logs --build-id 12345 --follow

Prerequisites:
    - Azure CLI (az) installed
    - Azure DevOps extension for Azure CLI
    - Valid Personal Access Token with build permissions
    - Access to dnceng-public/public project

Token Permissions Required:
    - Build (Read & Execute)
    - Code (Read)
    - Test Management (Read)
EOF
}

# Logging functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[SUCCESS]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[WARNING]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Check if Azure CLI is installed
check_azure_cli() {
    if ! command -v az &> /dev/null; then
        log_error "Azure CLI is not installed. Please install it first:"
        log_info "https://docs.microsoft.com/en-us/cli/azure/install-azure-cli"
        exit 1
    fi

    # Check if Azure DevOps extension is installed
    if ! az extension list | grep -q "azure-devops"; then
        log_info "Installing Azure DevOps extension..."
        az extension add --name azure-devops
    fi
}

# Setup authentication
setup_auth() {
    local token="$1"

    check_azure_cli

    log_info "Setting up Azure DevOps authentication..."

    # Configure defaults
    az devops configure --defaults organization=https://dev.azure.com/$ORGANIZATION project=$PROJECT

    # Login with PAT
    echo "$token" | az devops login --organization https://dev.azure.com/$ORGANIZATION

    log_success "Authentication configured successfully"
    log_info "Organization: $ORGANIZATION"
    log_info "Project: $PROJECT"
}

# Validate current authentication and access
validate_access() {
    check_azure_cli

    log_info "Validating Azure DevOps access..."

    # Test basic connectivity
    if ! az devops project show --project $PROJECT --organization https://dev.azure.com/$ORGANIZATION &>/dev/null; then
        log_error "Cannot access project. Please run 'setup' command first or check your token permissions."
        return 1
    fi

    # Check if we can access pipelines
    if ! az pipelines list --organization https://dev.azure.com/$ORGANIZATION --project $PROJECT &>/dev/null; then
        log_error "Cannot access pipelines. Check your token has Build permissions."
        return 1
    fi

    log_success "Access validation successful"
    return 0
}

# Get pipeline definition ID by name
get_pipeline_id() {
    local pipeline_name="$1"

    log_info "Finding pipeline: $pipeline_name"

    local pipeline_id
    pipeline_id=$(az pipelines list \
        --organization https://dev.azure.com/$ORGANIZATION \
        --project $PROJECT \
        --query "[?name=='$pipeline_name'].id | [0]" \
        --output tsv)

    if [ -z "$pipeline_id" ] || [ "$pipeline_id" = "None" ]; then
        log_error "Pipeline '$pipeline_name' not found"
        log_info "Available pipelines:"
        az pipelines list \
            --organization https://dev.azure.com/$ORGANIZATION \
            --project $PROJECT \
            --query "[].{Name:name, ID:id}" \
            --output table
        return 1
    fi

    echo "$pipeline_id"
}

# Trigger pipeline build
trigger_build() {
    local branch="$1"
    local wait_flag="$2"

    log_info "Triggering pipeline build on branch: $branch"

    # Get pipeline ID
    local pipeline_id
    if ! pipeline_id=$(get_pipeline_id "$PIPELINE_NAME"); then
        return 1
    fi

    log_info "Pipeline ID: $pipeline_id"

    # Trigger build
    local build_result
    build_result=$(az pipelines build queue \
        --organization https://dev.azure.com/$ORGANIZATION \
        --project $PROJECT \
        --definition-id "$pipeline_id" \
        --branch "$branch" \
        --output json)

    local build_id
    build_id=$(echo "$build_result" | jq -r '.id')

    if [ -z "$build_id" ] || [ "$build_id" = "null" ]; then
        log_error "Failed to trigger build"
        echo "$build_result" | jq '.'
        return 1
    fi

    local build_url
    build_url=$(echo "$build_result" | jq -r '.url')

    log_success "Build triggered successfully!"
    log_info "Build ID: $build_id"
    log_info "Build URL: $build_url"

    if [ "$wait_flag" = "true" ]; then
        log_info "Waiting for build completion..."
        wait_for_build "$build_id"
    else
        log_info "Use 'status --build-id $build_id' to check progress"
        log_info "Use 'logs --build-id $build_id --follow' to follow logs"
    fi

    echo "$build_id"
}

# Check build status
check_build_status() {
    local build_id="$1"

    log_info "Checking status of build: $build_id"

    local build_info
    build_info=$(az pipelines build show \
        --organization https://dev.azure.com/$ORGANIZATION \
        --project $PROJECT \
        --id "$build_id" \
        --output json)

    local status result started_time finished_time
    status=$(echo "$build_info" | jq -r '.status')
    result=$(echo "$build_info" | jq -r '.result // "running"')
    started_time=$(echo "$build_info" | jq -r '.startTime')
    finished_time=$(echo "$build_info" | jq -r '.finishTime // "N/A"')

    echo "Build Status Information:"
    echo "  Status: $status"
    echo "  Result: $result"
    echo "  Started: $started_time"
    echo "  Finished: $finished_time"

    if [ "$result" = "succeeded" ]; then
        log_success "Build completed successfully!"
        return 0
    elif [ "$result" = "failed" ]; then
        log_error "Build failed!"
        return 1
    elif [ "$result" = "canceled" ]; then
        log_warning "Build was canceled"
        return 1
    else
        log_info "Build is still running..."
        return 2
    fi
}

# Wait for build completion
wait_for_build() {
    local build_id="$1"
    local max_wait=3600  # 1 hour timeout
    local elapsed=0
    local interval=30

    log_info "Waiting for build $build_id to complete (timeout: ${max_wait}s)..."

    while [ $elapsed -lt $max_wait ]; do
        if check_build_status "$build_id" >/dev/null; then
            check_build_status "$build_id"
            return 0
        elif [ $? -eq 1 ]; then
            # Build failed
            check_build_status "$build_id"
            return 1
        fi

        sleep $interval
        elapsed=$((elapsed + interval))
        echo "  Waiting... (${elapsed}s elapsed)"
    done

    log_error "Timeout waiting for build completion"
    return 1
}

# Get build logs
get_build_logs() {
    local build_id="$1"
    local follow_flag="$2"

    if [ "$follow_flag" = "true" ]; then
        log_info "Following logs for build: $build_id (Ctrl+C to stop)"
        # Follow logs in real-time
        while true; do
            az pipelines build logs \
                --organization https://dev.azure.com/$ORGANIZATION \
                --project $PROJECT \
                --build-id "$build_id" 2>/dev/null || true
            sleep 10
        done
    else
        log_info "Getting logs for build: $build_id"
        az pipelines build logs \
            --organization https://dev.azure.com/$ORGANIZATION \
            --project $PROJECT \
            --build-id "$build_id"
    fi
}

# Get test results summary
get_test_results() {
    local build_id="$1"

    log_info "Getting test results for build: $build_id"

    # Get test runs for this build
    local test_runs
    test_runs=$(az test run list \
        --organization https://dev.azure.com/$ORGANIZATION \
        --project $PROJECT \
        --build-id "$build_id" \
        --output json 2>/dev/null || echo "[]")

    if [ "$test_runs" = "[]" ]; then
        log_warning "No test results found for build $build_id"
        return 0
    fi

    echo "Test Results Summary:"
    echo "$test_runs" | jq -r '.[] | "  Run: \(.name // "N/A") | Total: \(.totalTests // 0) | Passed: \(.passedTests // 0) | Failed: \(.unanalyzedTests // 0)"'

    # Get detailed results for failed tests
    local failed_count
    failed_count=$(echo "$test_runs" | jq '[.[].unanalyzedTests // 0] | add')

    if [ "$failed_count" -gt 0 ]; then
        log_warning "Found $failed_count failed tests"
        log_info "Use Azure DevOps web interface for detailed failure analysis"
    else
        log_success "All tests passed!"
    fi
}

# Main script logic
main() {
    # Parse command line arguments
    local command=""
    local branch=""
    local build_id=""
    local token="${AZDO_TOKEN:-}"
    local wait_flag="false"
    local follow_flag="false"

    while [[ $# -gt 0 ]]; do
        case $1 in
            trigger|status|logs|results|setup|validate)
                command="$1"
                shift
                ;;
            --branch)
                branch="$2"
                shift 2
                ;;
            --build-id)
                build_id="$2"
                shift 2
                ;;
            --token)
                token="$2"
                shift 2
                ;;
            --wait)
                wait_flag="true"
                shift
                ;;
            --follow)
                follow_flag="true"
                shift
                ;;
            --help|-h)
                show_help
                exit 0
                ;;
            *)
                log_error "Unknown option: $1"
                show_help
                exit 1
                ;;
        esac
    done

    # Handle commands
    case $command in
        setup)
            if [ -z "$token" ]; then
                log_error "Token required for setup. Use --token or set AZDO_TOKEN environment variable"
                exit 1
            fi
            setup_auth "$token"
            validate_access
            ;;
        validate)
            validate_access
            ;;
        trigger)
            if [ -z "$branch" ]; then
                log_error "Branch required for trigger command. Use --branch"
                exit 1
            fi
            validate_access || exit 1
            trigger_build "$branch" "$wait_flag"
            ;;
        status)
            if [ -z "$build_id" ]; then
                log_error "Build ID required for status command. Use --build-id"
                exit 1
            fi
            validate_access || exit 1
            check_build_status "$build_id"
            ;;
        logs)
            if [ -z "$build_id" ]; then
                log_error "Build ID required for logs command. Use --build-id"
                exit 1
            fi
            validate_access || exit 1
            get_build_logs "$build_id" "$follow_flag"
            ;;
        results)
            if [ -z "$build_id" ]; then
                log_error "Build ID required for results command. Use --build-id"
                exit 1
            fi
            validate_access || exit 1
            get_test_results "$build_id"
            ;;
        "")
            log_error "No command specified"
            show_help
            exit 1
            ;;
        *)
            log_error "Unknown command: $command"
            show_help
            exit 1
            ;;
    esac
}

# Run main function if script is executed directly
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi