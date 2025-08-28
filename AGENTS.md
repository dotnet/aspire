# AGENTS

1. Build: `./build.sh -restore -build` (CI: add `-ci -pack`). Use repo scripts: never invoke bare `dotnet`; use `./dotnet.sh`. Restore only: `./build.sh -restore`. Format/fix first if needed.
2. Single test project: `./dotnet.sh test tests/Project.Tests/Project.Tests.csproj -- --filter-not-trait "quarantined=true"`. Single test method: append `--filter-method "MethodName"` (combine with quarantine filter). Class filter: `--filter-class Namespace.TypeName`.
3. Exclude quarantined tests in automation: always add `--filter-not-trait "quarantined=true"`. Quarantined tests use `[QuarantinedTest("issue url")]`; they run separately in outerloop.
4. Snapshot tests: after changes run `./dotnet.sh test ...` then `./dotnet.sh verify accept -y` to accept new snapshots (Verify.XunitV3) when intentional.
5. VS Code extension (JS/TS): build/tests from `extension`: `yarn install && yarn test`; lint: `yarn lint`; package: `yarn package`.
6. Code style (C#): follow `.editorconfig`. File-scoped namespaces, System usings first, single-line usings, newline before braces, always braces even single line, prefer `var`, predefined types (`int` not `Int32`), pattern matching & switch expressions, use `nameof`, private/internal fields `_camelCase`, static fields `s_camelCase`, constants `PascalCase`.
7. Nullability: enable NRT; prefer non-nullable; use `is null` / `is not null`; do not add redundant null checks; forward `CancellationToken` parameters (CA2016).
8. Ordering: modifiers per editorconfig; place private nested types at file bottom; remove unused usings (except shared pattern exclusions). Prefer file-scoped namespace conversions (IDE0161).
9. Performance/diagnostics: heed CA18xx suggestions (collection count, spans, etc.); minimize allocations; prefer `TryGetValue`, `AsSpan`, `StartsWith` over `IndexOf` patterns.
10. Error handling: throw specific exceptions, preserve stack (`throw;`), avoid reserved exception types, correct argument exception construction (CA2208), use throw helpers (CA1510-13) when available.
11. Async: no `Task.WhenAll` for single task, avoid `WaitAll` on single, consider `ConfigureAwait` (warning suppressed in some UI/test files); avoid `WhenAll` with one element.
12. Formatting: no multiple blank lines, ensure final newline, UTF-8, 4-space indent (C#), 2-space JSON/XML; final return on its own line.
13. Tests: xUnit v3 + Microsoft.Testing.Platform; no mocking libs; no Arrange/Act/Assert comments; avoid `Directory.SetCurrentDirectory`; method names mimic neighbors; do not comment out tests.
14. Typescript (extension): naming convention for imports camelCase/PascalCase; enforce `curly`, `eqeqeq`, `no-throw-literal`, `semi`; use ES2022 modules.
15. Do NOT change: `global.json`, `NuGet.config`, `package.json`/lock files, generated `*/api/*.cs` unless explicitly instructed.
16. Build flags: temporary warning suppression allowed via `/p:TreatWarningsAsErrors=false` but must be reverted; fix warnings before final commit.
17. Filtering tests examples: method + quarantine exclusion: `./dotnet.sh test tests/Aspire.Hosting.Testing.Tests/Aspire.Hosting.Testing.Tests.csproj -- --filter-method "TestingBuilderHasAllPropertiesFromRealBuilder" --filter-not-trait "quarantined=true"`.
18. Accept style analyzers: address warnings (`IDE`, `CA`, custom) unless explicitly suppressed; do not introduce new warnings in committed code.
19. Markdown: no consecutive blank lines; code fences with language; tidy JSON indentation.
20. General: high-confidence changes only; follow repository scripts; prefer minimal diff respecting existing patterns.
