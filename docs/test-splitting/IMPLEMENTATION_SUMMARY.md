# Test Splitting Implementation - Summary & Checklist

(Your original summary content – truncated for brevity here; ensure full file if needed.)

Excerpt (lines you highlighted):

- [ ] **Create `eng/scripts/generate-test-matrix.ps1`**
  - [ ] Copy from STEP_03_MATRIX_GENERATOR_V3.md
  - [ ] Test with sample .tests.list files (see Step 5)
  - [ ] Verify JSON output is valid
  - [ ] Test both collection and class modes

- [ ] **Update `tests/Directory.Build.targets`**
  - [ ] Add enhanced ExtractTestClassNames target from STEP_02_MSBUILD_TARGETS_V3.md
  - [ ] Test locally with `dotnet build` (see Step 5)
  - [ ] Verify `.tests.list` and `.tests.metadata.json` are created
  - [ ] Check binlog for errors

(… rest of original summary …)