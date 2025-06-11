#!/usr/bin/env bash
#
# Git bisect helper script for investigating WithHttpCommand_ResultsInExpectedResultForHttpMethod test failures
# This script automates the git bisect process to find the commit that introduced repeated test failures.
#
# Usage: ./withhttpcommand-bisect.sh <good-commit> [bad-commit]
#   good-commit: A known good commit hash
#   bad-commit:  A known bad commit hash (defaults to HEAD)
#
# Example: ./withhttpcommand-bisect.sh abc123def main
#

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
TEST_PROJECT="tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj"
TEST_FILTER="WithHttpCommand_ResultsInExpectedResultForHttpMethod"
ITERATIONS=10

# Function to display usage
usage() {
    echo "Usage: $0 <good-commit> [bad-commit]"
    echo "  good-commit: A known good commit hash"
    echo "  bad-commit:  A known bad commit hash (defaults to HEAD)"
    echo ""
    echo "Example:"
    echo "  $0 abc123def"
    echo "  $0 abc123def main"
    exit 1
}

# Function to log messages with timestamps
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

# Function to run the test multiple times
run_test_iterations() {
    log "Running test $ITERATIONS times..."
    
    for i in $(seq 1 $ITERATIONS); do
        log "Iteration $i/$ITERATIONS"
        
        # Run the specific test with timeout
        if ! timeout 300 "$REPO_ROOT/dotnet.sh" test "$REPO_ROOT/$TEST_PROJECT" \
            --no-build \
            --logger "console;verbosity=quiet" \
            -- --filter "$TEST_FILTER" > /dev/null 2>&1; then
            log "Test failed on iteration $i (may have timed out after 5 minutes)"
            return 1
        fi
        
        # Small delay between iterations to avoid potential timing issues
        sleep 1
    done
    
    log "All $ITERATIONS iterations passed"
    return 0
}

# Function to build the project
build_project() {
    log "Building project..."
    if ! timeout 1800 "$REPO_ROOT/build.sh" --configuration Debug > /dev/null 2>&1; then
        log "Build failed (may have timed out after 30 minutes)"
        return 1
    fi
    log "Build successful"
    return 0
}

# Main function
main() {
    if [ $# -lt 1 ] || [ $# -gt 2 ]; then
        usage
    fi
    
    GOOD_COMMIT="$1"
    BAD_COMMIT="${2:-HEAD}"
    
    log "Starting git bisect for WithHttpCommand_ResultsInExpectedResultForHttpMethod test"
    log "Good commit: $GOOD_COMMIT"
    log "Bad commit: $BAD_COMMIT"
    log "Test iterations per commit: $ITERATIONS"
    
    cd "$REPO_ROOT"
    
    # Ensure we're in a clean state
    if ! git diff --quiet || ! git diff --cached --quiet; then
        log "Error: Repository has uncommitted changes. Please commit or stash them first."
        exit 1
    fi
    
    # Validate commits exist
    if ! git rev-parse --verify "$GOOD_COMMIT" >/dev/null 2>&1; then
        log "Error: Good commit '$GOOD_COMMIT' does not exist"
        exit 1
    fi
    
    if ! git rev-parse --verify "$BAD_COMMIT" >/dev/null 2>&1; then
        log "Error: Bad commit '$BAD_COMMIT' does not exist"
        exit 1
    fi
    
    # Store original branch/commit for cleanup
    ORIGINAL_REF=$(git symbolic-ref --short HEAD 2>/dev/null || git rev-parse HEAD)
    
    # Cleanup function
    cleanup() {
        log "Cleaning up..."
        git bisect reset >/dev/null 2>&1 || true
        if git rev-parse --verify "$ORIGINAL_REF" >/dev/null 2>&1; then
            git checkout "$ORIGINAL_REF" >/dev/null 2>&1 || true
        fi
        log "Repository state restored"
    }
    
    # Set up trap for cleanup on exit
    trap cleanup EXIT INT TERM
    
    # Start bisect
    log "Starting git bisect..."
    git bisect start
    git bisect bad "$BAD_COMMIT"
    git bisect good "$GOOD_COMMIT"
    
    # Create a temporary bisect test script
    BISECT_SCRIPT="/tmp/bisect-test-$$"
    cat > "$BISECT_SCRIPT" << 'EOF'
#!/usr/bin/env bash
set -euo pipefail

# Function to log messages with timestamps
log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*"
}

# Function to run the test multiple times
run_test_iterations() {
    log "Running test $ITERATIONS times..."
    
    for i in $(seq 1 $ITERATIONS); do
        log "Iteration $i/$ITERATIONS"
        
        # Run the specific test with timeout
        if ! timeout 300 "$REPO_ROOT/dotnet.sh" test "$REPO_ROOT/$TEST_PROJECT" \
            --no-build \
            --logger "console;verbosity=quiet" \
            -- --filter "$TEST_FILTER" > /dev/null 2>&1; then
            log "Test failed on iteration $i (may have timed out after 5 minutes)"
            return 1
        fi
        
        # Small delay between iterations to avoid potential timing issues
        sleep 1
    done
    
    log "All $ITERATIONS iterations passed"
    return 0
}

# Function to build the project
build_project() {
    log "Building project..."
    if ! timeout 1800 "$REPO_ROOT/build.sh" --configuration Debug > /dev/null 2>&1; then
        log "Build failed (may have timed out after 30 minutes)"
        return 1
    fi
    log "Build successful"
    return 0
}

# Bisect test function (used by git bisect run)
bisect_test() {
    log "Testing commit: $(git rev-parse --short HEAD)"
    
    # Try to build first
    if ! build_project; then
        log "Build failed - skipping this commit"
        exit 125  # Tell git bisect to skip this commit
    fi
    
    # Run the test iterations
    if run_test_iterations; then
        log "This commit is GOOD"
        exit 0  # Good commit
    else
        log "This commit is BAD"
        exit 1  # Bad commit
    fi
}

# Export variables
REPO_ROOT="$REPO_ROOT"
TEST_PROJECT="$TEST_PROJECT"
TEST_FILTER="$TEST_FILTER"
ITERATIONS="$ITERATIONS"

# Run the bisect test
bisect_test
EOF
    
    # Make the script executable
    chmod +x "$BISECT_SCRIPT"
    
    # Export variables for the script
    export REPO_ROOT TEST_PROJECT TEST_FILTER ITERATIONS
    
    # Update cleanup to remove the script
    cleanup() {
        log "Cleaning up..."
        git bisect reset >/dev/null 2>&1 || true
        if git rev-parse --verify "$ORIGINAL_REF" >/dev/null 2>&1; then
            git checkout "$ORIGINAL_REF" >/dev/null 2>&1 || true
        fi
        rm -f "$BISECT_SCRIPT"
        log "Repository state restored"
    }
    
    # Run the bisect
    log "Running automated bisect..."
    if ! git bisect run "$BISECT_SCRIPT"; then
        log "Bisect run failed or was interrupted"
        return 1
    fi
    
    # Show the result
    if git rev-parse --verify HEAD >/dev/null 2>&1; then
        log "Bisect completed!"
        log "The problematic commit is:"
        git show --no-patch --format="%H %s" HEAD
        
        # Save bisect log
        BISECT_LOG="$REPO_ROOT/bisect-withhttpcommand-$(date +%Y%m%d-%H%M%S).log"
        git bisect log > "$BISECT_LOG"
        log "Bisect log saved to: $BISECT_LOG"
    else
        log "Bisect may not have completed successfully. Check the git state."
    fi
}

# Check if this script is being called by git bisect run
if [ "${1:-}" = "bisect_test" ]; then
    # This should not happen anymore as we use a separate script
    echo "Error: This script should not be called with bisect_test argument anymore"
    exit 1
else
    main "$@"
fi