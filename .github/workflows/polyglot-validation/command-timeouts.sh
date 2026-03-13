#!/bin/bash

command_timeout_timestamp() {
    date -u +"%H:%M:%S"
}

run_with_timeout() {
    local timeout_seconds="$1"
    shift

    if command -v timeout &> /dev/null; then
        timeout --foreground "${timeout_seconds}s" "$@"
        return $?
    fi

    node - "$timeout_seconds" "$@" <<'NODE'
const { spawn } = require('node:child_process');

const [, , timeoutSecondsText, command, ...args] = process.argv;
const timeoutSeconds = Number(timeoutSecondsText);
const child = spawn(command, args, { stdio: 'inherit' });

let timedOut = false;
const timeoutHandle = setTimeout(() => {
    timedOut = true;
    console.error(`❌ Command timed out after ${timeoutSeconds}s: ${command} ${args.join(' ')}`);
    child.kill('SIGTERM');
    setTimeout(() => child.kill('SIGKILL'), 5000).unref();
}, timeoutSeconds * 1000);

child.on('error', (error) => {
    clearTimeout(timeoutHandle);
    console.error(`❌ Failed to start command: ${error.message}`);
    process.exit(1);
});

child.on('exit', (code, signal) => {
    clearTimeout(timeoutHandle);

    if (timedOut || signal) {
        process.exit(124);
    }

    process.exit(code ?? 1);
});
NODE
}

run_logged_command() {
    local label="$1"
    local timeout_seconds="$2"
    shift 2

    local start_seconds=$SECONDS

    echo "  → [$(command_timeout_timestamp)] ${label} (timeout ${timeout_seconds}s)..."

    run_with_timeout "$timeout_seconds" "$@"
    local status=$?
    local duration_seconds=$((SECONDS - start_seconds))

    if [ "$status" -eq 0 ]; then
        echo "  ✓ ${label} completed in ${duration_seconds}s"
        return 0
    fi

    if [ "$status" -eq 124 ]; then
        echo "  ❌ ${label} timed out after ${duration_seconds}s (limit ${timeout_seconds}s)"
    else
        echo "  ❌ ${label} failed after ${duration_seconds}s (exit ${status})"
    fi

    return "$status"
}
