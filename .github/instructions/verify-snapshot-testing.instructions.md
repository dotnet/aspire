---
applyTo: "tests/**"
---

# Snapshot Testing (Verify)

This project uses [Verify](https://github.com/VerifyTests/Verify) for snapshot testing.

## Key concepts

- **`.verified.*` files** are the approved snapshots. They are committed to source control.
- **`.received.*` files** are the actual output from the latest test run. They are generated when a test fails (actual != expected) and are git-ignored.
- When a test fails, compare the `.received.*` file to the `.verified.*` file to understand the difference.

## Handling test failures

When a snapshot test fails:

1. Run the failing test and read the `.received.*` file that was generated.
2. Compare it to the corresponding `.verified.*` file.
3. Determine if the difference is expected (due to an intentional code change) or a bug.
   - **If expected**: accept the new snapshot with `dotnet verify accept -y` or copy the `.received.*` file over the `.verified.*` file.
   - **If a bug**: fix the code, not the snapshot.
4. Never hand-edit `.verified.*` files to make tests pass. Always let Verify generate the correct output by running the test.

## Exception message format

When a test fails, the exception message is machine-parsable:

```
Directory: /path/to/test/project
New:
  - Received: TestClass.Method.received.txt
    Verified: TestClass.Method.verified.txt
NotEqual:
  - Received: TestClass.Method.received.txt
    Verified: TestClass.Method.verified.txt

FileContent:

New:

Received: TestClass.Method.received.txt
<file content here>

NotEqual:

Received: TestClass.Method.received.txt
<received content>
Verified: TestClass.Method.verified.txt
<verified content>
```

- The `Directory:` line gives the base path for all relative file references.
- `New` means no `.verified.` file exists yet (first run or new test).
- `NotEqual` means the `.received.` and `.verified.` files differ.
- `Delete` means a `.verified.` file is no longer produced by any test.
- The `FileContent:` section contains the actual text content for quick comparison without needing to read files separately.

## Scrubbed values

Verify replaces non-deterministic values in snapshots with stable placeholders:

- GUIDs become `Guid_1`, `Guid_2`, etc.
- DateTimes become `DateTime_1`, `DateTime_2`, etc.
- File paths are replaced with tokens like `{SolutionDirectory}`, `{ProjectDirectory}`, `{TempPath}`.
- Custom scrubbers may replace other values.

These placeholders are intentional and ensure snapshots are deterministic across machines and runs.

## Verified file conventions

- Encoding: UTF-8 with BOM
- Line endings: LF (not CRLF)
- No trailing newline

## Common patterns

```csharp
// Verify an object (serialized to JSON)
await Verify(myObject);

// Verify a string
await Verify("some text output");

// Verify with inline settings
await Verify(myObject)
    .ScrubLines(line => line.Contains("volatile"))
    .DontScrubDateTimes();

// Verify throws
await ThrowsTask(() => MethodThatThrows())
    .IgnoreStackTrace();
```
