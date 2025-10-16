# Step 2: MSBuild Targets (v3 Auto-Detection)

Describe enhanced ExtractTestClassNames target:
- Gated by SplitTestsOnCI
- Runs dotnet test assembly with --list-tests
- Captures output, passes to extract-test-metadata.ps1
- Writes .tests.list and .tests.metadata.json
- Fails build if prefix missing or zero tests discovered

(You can request full expanded version; I have it ready.)