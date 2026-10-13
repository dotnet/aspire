---
applyTo: ".github/workflows/**/*,eng/**/*,tests/Infrastructure.Tests/**/*,tests/Shared/**/*"
---

# Infrastructure.Tests authoring and review guidance

## Core rule

Test executable behavior, not how its source is written. Do not assert literal source text in YAML, project files, scripts, or Markdown. Do not extract or evaluate workflow YAML in tests, or add YAML-only configuration tests. Parsing YAML, comparing nodes across files, or snapshotting configuration does not make these tests behavioral.

Name the exact failure each test catches and the observable result that would change. Remove source-pinning and configuration-restatement tests; retain any separable assertions that execute production logic. Do not replace deleted tests with a test-only implementation of the same logic.

## Configuration and platform behavior

- Use actionlint for GitHub Actions syntax, expressions, and schema; use the appropriate validator for other formats. A successful lint does not prove job scheduling, skipped-dependency propagation, artifact availability, permissions, or final-result behavior.
- For configuration-only changes, review the complete producer-consumer graph and use existing execution evidence for the affected scenarios. Record platform behavior that remains unvalidated. Do not add configuration assertions just to give every change a unit test.
- Security-sensitive configuration and cross-file relationships are not exceptions to the rule. Review them directly, use appropriate tooling, and validate platform behavior rather than asserting configuration text or parsed nodes.
- Configuration may be input to an actual production parser, selector, build, or generator. Invoke that implementation and assert its output or failure, not the input's declarations. For example, passing a changed workflow path to the production test selector and checking its selected targets tests the selector; parsing the workflow's job conditions in the test does not test scheduling.

## Imperative logic

- Extract inline JavaScript, PowerShell, or shell when it contains meaningful branches, loops, parsing or transformation, retry or error handling, security-sensitive decisions, or reusable logic.
- Execute the committed script directly with controlled fixtures. Assert exit codes, complete outputs, side effects, generated files, and external-tool invocations. Reuse `NodeCommand`, `PowerShellCommand`, temporary workspaces, fake executables through `PATH`, and generated fixtures.
- Keep trivial orchestration commands and platform-owned job expressions inline. Do not move configuration into a new script or build a workflow-expression interpreter solely to retain deleted tests.
- For executable gates and result validators, test missing, malformed, unknown, empty, and mixed valid and invalid evidence. One valid input must not mask corrupt evidence, and advisory handling must remain limited to explicitly recognized outcomes.
- Review workflow invocation, inputs, permissions, dependencies, and conditions directly; validate them with tooling or platform execution. Do not add parsed-YAML wiring tests after script extraction.

## Fixtures and generated output

- Use a temporary workspace for ordinary file, script, or MSBuild fixtures. Initialize a minimal Git repository only when behavior depends on commits, branches, merge bases, rename records, staging, or `git diff`; assert the resulting behavior rather than Git command text.
- Use Verify for stable, complete artifacts produced by executing the real generator. Generated output is different from reading or snapshotting checked-in configuration. Keep focused assertions for calculations, filtering, security or escaping, and edge behavior; avoid large approval blobs.
- Test selector and trigger-map mechanisms by invoking the production implementation with small synthetic maps and project graphs. Keep real-map coverage to representative positive and negative regressions for consequential routing boundaries; assert selected targets, not rule declarations or workflow inventories.

## Authoring and review check

1. Identify the changed executable behavior and platform-owned configuration separately.
2. For each test, identify the production implementation it executes and a concrete regression that changes the assertion. Names such as "semantic", "contract", or "relationship" are not evidence of behavioral coverage.
3. Remove prohibited assertions. Preserve a useful behavioral part independently or merge a new case into its natural existing test; do not refactor unrelated pre-existing tests.
4. Check meaningful mutations where feasible. A test that stays green when its decision is inverted or its failure gate is disabled does not protect that decision. Do not fix this by pinning more source text.
5. Run affected executable tests and configuration validators. Report removals, retained failure modes, and remaining platform-validation gaps separately; fewer tests or passing tests alone is not success.

Live or dry-run validation must exercise the affected scenario, not merely a full run that never skips an optional producer. Azure Pipelines changes that depend on platform behavior still require validation in the official pipeline. Neither instruction changes nor a testing recommendation authorize remote runs or publication.
