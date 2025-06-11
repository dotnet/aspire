# Git Bisect Helper Scripts

This directory contains automated git bisect scripts for investigating test failures in the Aspire repository.

## Available Scripts

- `withhttpcommand-bisect.sh` - Unix/macOS/Linux script for investigating WithHttpCommand_ResultsInExpectedResultForHttpMethod test failures
- `withhttpcommand-bisect.cmd` - Windows script for investigating WithHttpCommand_ResultsInExpectedResultForHttpMethod test failures

## Documentation

See [docs/bisect-withhttpcommand.md](../../docs/bisect-withhttpcommand.md) for detailed usage instructions.

## Quick Usage

```bash
# Unix/macOS/Linux
./withhttpcommand-bisect.sh <good-commit> [bad-commit]

# Windows
withhttpcommand-bisect.cmd <good-commit> [bad-commit]
```

Where:
- `<good-commit>` is a known good commit where the test was passing consistently
- `[bad-commit]` is a known bad commit where the test fails (defaults to HEAD)