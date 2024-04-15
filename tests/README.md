# Helix

The helix CI job builds `tests/helix/send-to-helix-ci.proj`, which in turns builds the `Test` target on `tests/helix/send-to-helix-inner.proj`. This inner project uses the Helix SDK to construct `@(HelixWorkItem)`s, and send them to helix to run.

- `tests/helix/send-to-helix-basic-tests.targets` - this prepares all the tests that don't need special preparation
- `tests/helix/send-to-helix-workload-tests.targets` - this is for tests that require a sdk+workload installed
