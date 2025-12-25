#!/usr/bin/env python3
"""Aspire Python AppHost for testing."""

import os
import signal
import sys
import time

print("Aspire Python AppHost starting...")
print(f"Environment: {os.environ.get('PYTHON_ENV', 'development')}")

start_time = time.time()

print("AppHost is running. Press Ctrl+C to stop.")

running = True

def signal_handler(signum, frame):
    global running
    print("\nReceived signal. Shutting down gracefully...")
    running = False

signal.signal(signal.SIGINT, signal_handler)
signal.signal(signal.SIGTERM, signal_handler)

try:
    while running:
        elapsed = int(time.time() - start_time)
        print(f"AppHost running for {elapsed} seconds...")
        time.sleep(5)
except KeyboardInterrupt:
    pass

print("AppHost stopped.")
sys.exit(0)
