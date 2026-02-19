#!/bin/bash

# Script to run flaky test 100 times to verify the fix
# This helps ensure the fix for issue #9673 is stable
# BREAKS ON FIRST FAILURE for immediate investigation

set -u  # Exit on undefined variables
set -E  # Inherit ERR trap

# Configuration
TEST_PROJECT="tests/Aspire.Hosting.Tests/Aspire.Hosting.Tests.csproj"
TEST_METHOD="*.TestPortOnEndpointAnnotationAndAllocatedEndpointAnnotationMatchForReplicatedServices"
ITERATIONS=100
RESULTS_DIR="/tmp/test-results-$(date +%Y%m%d-%H%M%S)"
LOG_FILE="$RESULTS_DIR/test-run.log"

# Statistics
PASS_COUNT=0
FAIL_COUNT=0
ERROR_COUNT=0

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Create results directory
mkdir -p "$RESULTS_DIR"

# Save command line for reproducibility
echo "Command line: $0 $@" > "$RESULTS_DIR/command.txt"
echo "Working directory: $(pwd)" >> "$RESULTS_DIR/command.txt"
echo "Git commit: $(git rev-parse HEAD 2>/dev/null || echo 'N/A')" >> "$RESULTS_DIR/command.txt"
echo "Started: $(date)" >> "$RESULTS_DIR/command.txt"

echo "========================================" | tee -a "$LOG_FILE"
echo "Test Verification Run - 100 Iterations" | tee -a "$LOG_FILE"
echo "BREAK ON FIRST FAILURE MODE" | tee -a "$LOG_FILE"
echo "========================================" | tee -a "$LOG_FILE"
echo "Test: $TEST_METHOD" | tee -a "$LOG_FILE"
echo "Project: $TEST_PROJECT" | tee -a "$LOG_FILE"
echo "Results: $RESULTS_DIR" | tee -a "$LOG_FILE"
echo "Started: $(date)" | tee -a "$LOG_FILE"
echo "Command: $0 $@" | tee -a "$LOG_FILE"
echo "Git commit: $(git rev-parse HEAD 2>/dev/null || echo 'N/A')" | tee -a "$LOG_FILE"
echo "========================================" | tee -a "$LOG_FILE"
echo "" | tee -a "$LOG_FILE"

# Function to clean resources between runs
clean_resources() {
    local iteration=$1
    
    # Kill any remaining test processes
    pkill -9 -f "Aspire.Hosting.Tests" 2>/dev/null || true
    pkill -9 -f "TestProject.Service" 2>/dev/null || true
    pkill -9 -f "TestProject.Worker" 2>/dev/null || true
    
    # Clean up DCP resources if running
    pkill -9 -f "dcp" 2>/dev/null || true
    
    # Clean test result files
    rm -rf tests/Aspire.Hosting.Tests/TestResults 2>/dev/null || true
    rm -rf artifacts/bin/Aspire.Hosting.Tests/Debug/net8.0/TestResults 2>/dev/null || true
    
    # Wait a moment for cleanup
    sleep 1
}

# Function to run a single test iteration
run_test() {
    local iteration=$1
    local test_log="$RESULTS_DIR/iteration-$iteration.log"
    
    echo -n "Iteration $iteration/$ITERATIONS: " | tee -a "$LOG_FILE"
    
    # Run the test with timeout
    timeout 180 dotnet test "$TEST_PROJECT" \
        --no-build \
        --no-restore \
        -- \
        --filter-method "$TEST_METHOD" \
        > "$test_log" 2>&1
    
    local exit_code=$?
    
    # Analyze result
    if [ $exit_code -eq 0 ]; then
        if grep -q "Passed!" "$test_log" || grep -q "total: 1" "$test_log" && grep -q "succeeded: 1" "$test_log"; then
            echo -e "${GREEN}PASS${NC}" | tee -a "$LOG_FILE"
            PASS_COUNT=$((PASS_COUNT + 1))
            return 0
        else
            echo -e "${RED}FAIL${NC} (exit 0 but no pass indicator)" | tee -a "$LOG_FILE"
            FAIL_COUNT=$((FAIL_COUNT + 1))
            # Save failure log
            cp "$test_log" "$RESULTS_DIR/failure-$iteration.log"
            return 1
        fi
    elif [ $exit_code -eq 124 ]; then
        echo -e "${YELLOW}TIMEOUT${NC}" | tee -a "$LOG_FILE"
        ERROR_COUNT=$((ERROR_COUNT + 1))
        cp "$test_log" "$RESULTS_DIR/timeout-$iteration.log"
        return 1
    else
        echo -e "${RED}FAIL${NC} (exit code: $exit_code)" | tee -a "$LOG_FILE"
        FAIL_COUNT=$((FAIL_COUNT + 1))
        cp "$test_log" "$RESULTS_DIR/failure-$iteration.log"
        return 1
    fi
}

# Main test loop
echo "Starting test iterations..." | tee -a "$LOG_FILE"
echo "Mode: BREAK ON FIRST FAILURE" | tee -a "$LOG_FILE"
echo "" | tee -a "$LOG_FILE"

FIRST_FAILURE_ITERATION=0

for i in $(seq 1 $ITERATIONS); do
    # Clean before each run
    clean_resources $i
    
    # Run the test
    if ! run_test $i; then
        # Test failed - break immediately
        FIRST_FAILURE_ITERATION=$i
        echo "" | tee -a "$LOG_FILE"
        echo -e "${RED}========================================${NC}" | tee -a "$LOG_FILE"
        echo -e "${RED}FAILURE DETECTED - Breaking on iteration $i${NC}" | tee -a "$LOG_FILE"
        echo -e "${RED}========================================${NC}" | tee -a "$LOG_FILE"
        echo "" | tee -a "$LOG_FILE"
        
        # Save failure details
        echo "First failure at iteration: $i" >> "$RESULTS_DIR/failure-info.txt"
        echo "Pass count before failure: $PASS_COUNT" >> "$RESULTS_DIR/failure-info.txt"
        echo "Failure count: $FAIL_COUNT" >> "$RESULTS_DIR/failure-info.txt"
        echo "Error count: $ERROR_COUNT" >> "$RESULTS_DIR/failure-info.txt"
        
        break
    fi
    
    # Show progress every 10 iterations
    if [ $((i % 10)) -eq 0 ]; then
        echo "" | tee -a "$LOG_FILE"
        echo -e "${BLUE}Progress: $i/$ITERATIONS completed${NC}" | tee -a "$LOG_FILE"
        echo "  Pass: $PASS_COUNT, Fail: $FAIL_COUNT, Errors: $ERROR_COUNT" | tee -a "$LOG_FILE"
        echo "" | tee -a "$LOG_FILE"
    fi
done

# Final cleanup
clean_resources "final"

# Generate summary report
TOTAL_RUNS=$((PASS_COUNT + FAIL_COUNT + ERROR_COUNT))
echo "" | tee -a "$LOG_FILE"
echo "========================================" | tee -a "$LOG_FILE"
echo "Test Results Summary" | tee -a "$LOG_FILE"
echo "========================================" | tee -a "$LOG_FILE"
if [ $FIRST_FAILURE_ITERATION -gt 0 ]; then
    echo -e "${RED}STOPPED EARLY at iteration $FIRST_FAILURE_ITERATION due to failure${NC}" | tee -a "$LOG_FILE"
fi
echo "Total Iterations Completed: $TOTAL_RUNS / $ITERATIONS" | tee -a "$LOG_FILE"
echo -e "${GREEN}Passed:  $PASS_COUNT${NC} ($(awk "BEGIN {printf \"%.1f\", ($PASS_COUNT/$TOTAL_RUNS)*100}")%)" | tee -a "$LOG_FILE"
echo -e "${RED}Failed:  $FAIL_COUNT${NC} ($(awk "BEGIN {printf \"%.1f\", ($FAIL_COUNT/$TOTAL_RUNS)*100}")%)" | tee -a "$LOG_FILE"
echo -e "${YELLOW}Errors:  $ERROR_COUNT${NC} ($(awk "BEGIN {printf \"%.1f\", ($ERROR_COUNT/$TOTAL_RUNS)*100}")%)" | tee -a "$LOG_FILE"
echo "========================================" | tee -a "$LOG_FILE"
echo "Completed: $(date)" | tee -a "$LOG_FILE"
echo "Results saved to: $RESULTS_DIR" | tee -a "$LOG_FILE"
echo "" | tee -a "$LOG_FILE"

# Create summary file
cat > "$RESULTS_DIR/summary.txt" << EOF
Test Verification Summary
=========================
Test: $TEST_METHOD
Mode: BREAK ON FIRST FAILURE
Planned Iterations: $ITERATIONS
Completed Iterations: $TOTAL_RUNS
Started: $(head -12 "$LOG_FILE" | tail -1 | cut -d' ' -f2-)
Completed: $(date)

Results:
--------
Passed:  $PASS_COUNT / $TOTAL_RUNS ($(awk "BEGIN {printf \"%.1f\", ($PASS_COUNT/$TOTAL_RUNS)*100}")%)
Failed:  $FAIL_COUNT / $TOTAL_RUNS ($(awk "BEGIN {printf \"%.1f\", ($FAIL_COUNT/$TOTAL_RUNS)*100}")%)
Errors:  $ERROR_COUNT / $TOTAL_RUNS ($(awk "BEGIN {printf \"%.1f\", ($ERROR_COUNT/$TOTAL_RUNS)*100}")%)

Status: $([ $FAIL_COUNT -eq 0 ] && [ $ERROR_COUNT -eq 0 ] && [ $TOTAL_RUNS -eq $ITERATIONS ] && echo "SUCCESS - All $ITERATIONS tests passed!" || echo "FAILURE - Test stopped at iteration $FIRST_FAILURE_ITERATION")

Previous failure rate (before fix): ~23.5%
Expected failure rate (with fix): 0%

Analysis:
---------
$(if [ $FAIL_COUNT -eq 0 ] && [ $ERROR_COUNT -eq 0 ] && [ $TOTAL_RUNS -eq $ITERATIONS ]; then
    echo "✓ Fix verified! No failures in $ITERATIONS iterations."
    echo "✓ The WaitForHealthyAsync approach eliminates the race condition."
elif [ $FIRST_FAILURE_ITERATION -gt 0 ]; then
    echo "✗ FAILURE on iteration $FIRST_FAILURE_ITERATION"
    echo "  Test stopped early for investigation."
    echo "  Pass count before failure: $PASS_COUNT"
    echo "  Review failure-$FIRST_FAILURE_ITERATION.log for details"
elif [ $FAIL_COUNT -lt 10 ]; then
    echo "⚠ Minor failures detected. Review failure logs."
    echo "  Previous rate was ~23.5%, current is $(awk "BEGIN {printf \"%.1f\", ($FAIL_COUNT/$TOTAL_RUNS)*100}")%"
else
    echo "✗ Significant failures still occurring."
    echo "  Additional investigation needed."
fi)

Log files:
----------
Main log: $LOG_FILE
Command info: $RESULTS_DIR/command.txt
Failure logs: $RESULTS_DIR/failure-*.log
Timeout logs: $RESULTS_DIR/timeout-*.log
$([ $FIRST_FAILURE_ITERATION -gt 0 ] && echo "Failure info: $RESULTS_DIR/failure-info.txt")
EOF

cat "$RESULTS_DIR/summary.txt"

# Exit with appropriate code
if [ $FAIL_COUNT -eq 0 ] && [ $ERROR_COUNT -eq 0 ] && [ $TOTAL_RUNS -eq $ITERATIONS ]; then
    echo ""
    echo -e "${GREEN}✓ SUCCESS: All $ITERATIONS iterations passed!${NC}"
    exit 0
else
    echo ""
    echo -e "${RED}✗ FAILURE: Stopped at iteration $FIRST_FAILURE_ITERATION${NC}"
    echo -e "${RED}  Failures: $FAIL_COUNT, Errors: $ERROR_COUNT${NC}"
    if [ -f "$RESULTS_DIR/failure-info.txt" ]; then
        echo ""
        echo "Failure details:"
        cat "$RESULTS_DIR/failure-info.txt"
    fi
    exit 1
fi
