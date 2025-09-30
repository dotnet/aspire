# Quarantine Tools

Roslyn based utility to add or remove `[QuarantinedTest]` attributes on test methods.

- Quarantine: adds `[QuarantinedTest("<issue url>")]`
- Unquarantine: removes any `QuarantinedTest` attribute from the method

## Prerequisites

- .NET SDK 10+ (for source-file `dotnet run`)

## Preferred usage (dotnet run)

```bash
# Show help
dotnet run --project tools/QuarantineTools -- --help

# Quarantine a test (adds attribute with issue URL)
dotnet run --project tools/QuarantineTools -- -q -i https://github.com/dotnet/aspire/issues/1234 Full.Namespace.Type.Method

# Unquarantine a test (removes attribute)
dotnet run --project tools/QuarantineTools -- -u Full.Namespace.Type.Method

# Specify a custom tests root folder (defaults to <repo-root>/tests)
dotnet run --project tools/QuarantineTools -- -q -r tests/MySubset -i https://github.com/org/repo/issues/1 N1.N2.C.M

# Use a custom attribute full name (defaults to Aspire.TestUtilities.QuarantinedTest)
dotnet run --project tools/QuarantineTools -- -q -a MyCompany.Testing.CustomQuarantinedTest -i https://example.com/issue/1 N1.N2.C.M
```

## Notes on CLI flags

- `-q`/`--quarantine` and `-u`/`--unquarantine` are mutually exclusive (pick one).
- `-i`/`--url <issue-url>` is required when using `-q`.
- `-a`/`--attribute <Full.Name>` controls which attribute is added/removed. The tool accepts name with or without the `Attribute` suffix. If a namespace is supplied, the tool adds a `using` directive when inserting the short attribute name.
- `-r`/`--root <folder>` overrides the scan root (by default, `<repo-root>/tests`). Relative paths are resolved against the repository root.
- `-h`/`--help` prints usage information and exits.

## Notes

- Methods are matched by fully-qualified name: `Namespace.Type.Method`. Nested types can use `+`, e.g. `Namespace.Outer+Inner.TestMethod`.
- The tool scans the repo `tests/` folder and edits files in place, ignoring `bin/` and `obj/`.
- Quarantine is idempotent (won't duplicate attributes); unquarantine only removes the attribute if present.
