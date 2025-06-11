#!/bin/bash
# Test script to validate quarantine filtering is working correctly

set -e

echo "Validating quarantine filtering for Aspire.Cli.Tests..."

# Build the test project (skip for now since we know it's built)
# echo "Building test project..."
# ./dotnet.sh build tests/Aspire.Cli.Tests/Aspire.Cli.Tests.csproj --no-restore -q

# Get the test assembly path
TEST_ASSEMBLY="artifacts/bin/Aspire.Cli.Tests/Debug/net8.0/Aspire.Cli.Tests.dll"

echo "Running all tests..."
ALL_TESTS=$(./dotnet.sh exec "$TEST_ASSEMBLY" --list-tests | grep -c "Aspire.Cli.Tests" || true)

echo "Running tests without quarantined..."
NON_QUARANTINED_TESTS=$(./dotnet.sh exec "$TEST_ASSEMBLY" --filter-not-trait "quarantined=true" --list-tests | grep -c "Aspire.Cli.Tests" || true)

echo "Running only quarantined tests..."
QUARANTINED_TESTS=$(./dotnet.sh exec "$TEST_ASSEMBLY" --filter-trait "quarantined=true" --list-tests | grep -c "Aspire.Cli.Tests" || true)

echo "Results:"
echo "  Total tests: $ALL_TESTS"
echo "  Non-quarantined tests: $NON_QUARANTINED_TESTS"
echo "  Quarantined tests: $QUARANTINED_TESTS"

# Validate the math
EXPECTED_TOTAL=$((NON_QUARANTINED_TESTS + QUARANTINED_TESTS))
if [ "$ALL_TESTS" -ne "$EXPECTED_TOTAL" ]; then
    echo "ERROR: Test counts don't add up! Expected $ALL_TESTS to equal $EXPECTED_TOTAL"
    exit 1
fi

if [ "$QUARANTINED_TESTS" -eq 0 ]; then
    echo "WARNING: No quarantined tests found. This test project may not have any quarantined tests."
fi

echo "✅ Quarantine filtering validation passed!"
echo "  - Regular CI should use: --filter-not-trait \"quarantined=true\""
echo "  - To debug quarantined tests: --filter-trait \"quarantined=true\""