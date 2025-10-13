# Azure DevOps Matrix Testing Migration Design

## Overview
Migrate Azure DevOps testing from Helix-based execution to direct pipeline execution using matrix jobs, similar to the GitHub Actions approach.

**⚠️ IMPORTANT**: This project targets the **PUBLIC PIPELINE ONLY** (`dnceng-public/public`). Do NOT modify the official/internal builds pipeline.

## Current State Analysis

### Pipeline Structure
- **Main Pipeline** (`azure-pipelines.yml`): Official builds - **NOT MODIFIED**
- **Public Pipeline** (`azure-pipelines-public.yml`): Scheduled builds using public-pipeline-template
- **Manual Pipeline** (`azdo-tests.yml`): Manual trigger for testing
- **Template** (`public-pipeline-template.yml`): Shared template supporting test variants

### Current Test Execution
- **Helix Tests**: Sent to remote Helix agents via `send-to-helix.yml`
- **Pipeline Tests**: Run directly on Azure DevOps agents
- **Test Variants**: `_pipeline_tests`, `_helix_tests` control execution mode
- **Monolithic Jobs**: Single job per platform runs ALL tests

### Current Test Discovery
- Uses `GetTestProjects.proj` to enumerate test projects
- Same logic as GitHub Actions but executed differently
- Generates "short names" like `Dashboard`, `Hosting`, `Components.Tests`, etc.

## Target State Design

### Goals
1. **Eliminate Helix Dependency**: All tests run on Azure DevOps build agents
2. **Parallel Test Execution**: Individual test projects run as separate jobs
3. **Platform Matrix**: Tests run on both Linux and Windows
4. **Consistent with GitHub Actions**: Same test filtering and structure

### Architecture

```
Current:
├── Windows_pipeline_tests (runs ALL tests)
├── Linux_pipeline_tests (runs ALL tests)
├── Windows_helix_tests (sends ALL tests to Helix)
└── Linux_helix_tests (sends ALL tests to Helix)

Target (Phase 1):
├── IntegrationTests_Linux_Dashboard
├── IntegrationTests_Linux_Hosting
├── IntegrationTests_Linux_Components.Tests
├── IntegrationTests_Windows_Dashboard
├── IntegrationTests_Windows_Hosting
├── IntegrationTests_Windows_Components.Tests
└── [Keep existing approach for Templates/EndToEnd temporarily]
```

### Technical Implementation

#### Test Enumeration
- Reuse `tests/Shared/GetTestProjects.proj` logic
- Generate matrix of test projects dynamically
- Filter out excluded projects (Templates, EndToEnd, etc.)

#### Matrix Job Structure
```yaml
strategy:
  matrix:
    Dashboard_Linux:
      testShortName: 'Dashboard'
      imageName: 'build.ubuntu.2204.amd64.open'
      scriptName: 'dotnet.sh'
    Dashboard_Windows:
      testShortName: 'Dashboard'
      imageName: 'windows.vs2022preview.amd64.open'
      scriptName: 'dotnet.cmd'
    # ... more combinations
```

#### Test Execution
- Each matrix job runs a single test project
- Same test filtering as GitHub Actions: `--filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"`
- Timeout per job: 15-20 minutes (vs current 90 minutes for all tests)

## Phased Implementation Plan

### Phase 1: Integration Tests Matrix
**Scope**: Replace Helix integration tests with matrix jobs
- Enumerate integration test projects using existing `GetTestProjects.proj`
- Create Linux and Windows matrix jobs for each test project
- Disable Helix tests for integration tests
- Keep existing approach for Template and EndToEnd tests

**Files Modified**:
- `eng/pipelines/templates/public-pipeline-template.yml`
- Create new template: `eng/pipelines/templates/integration-tests-matrix.yml`

### Phase 2: Template Tests Matrix (Future)
**Scope**: Migrate template tests to matrix approach
- Similar matrix structure but for template test classes
- Uses different test project path and filtering

### Phase 3: EndToEnd Tests (Future)
**Scope**: Integrate EndToEnd tests into matrix
- Linux only (Docker dependency)
- May need special handling for package dependencies

## Risk Analysis

### Benefits
- **Faster Feedback**: Parallel execution reduces total build time
- **Better Isolation**: Test failures don't affect other test projects
- **Resource Efficiency**: No Helix queue wait times
- **Consistency**: Matches GitHub Actions approach

### Risks
- **Build Agent Usage**: More parallel jobs = more agent usage
- **Complexity**: More moving parts in pipeline configuration
- **Debugging**: Individual job failures harder to correlate

### Mitigation
- Start with Phase 1 only to validate approach
- Monitor build agent usage and adjust if needed
- Maintain good job naming for easier debugging

## Configuration Changes

### Test Filtering
Maintain same filtering as GitHub Actions:
```bash
--filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

### Timeout Strategy
- Individual jobs: 15-20 minutes (current per-test average)
- Overall pipeline: Similar to current (~90 minutes total)

### Platform Support
- **Linux**: `build.ubuntu.2204.amd64.open`
- **Windows**: `windows.vs2022preview.amd64.open`
- **macOS**: Not needed for Azure DevOps (GitHub Actions covers this)

## Success Criteria

### Phase 1 Success
- [ ] Integration tests run as individual matrix jobs
- [ ] Both Linux and Windows platforms supported
- [ ] Test results properly collected and reported
- [ ] Build time comparable or improved vs current
- [ ] No test regressions vs GitHub Actions results

### Monitoring
- Build duration metrics
- Test pass rates
- Build agent usage patterns
- Any test stability issues

## Future Considerations

### Potential Optimizations
- **Smart Matrix**: Only run tests for changed areas
- **Artifact Caching**: Share build artifacts across matrix jobs
- **Dynamic Scaling**: Adjust matrix size based on changes

### Integration with Existing Tools
- Maintain compatibility with existing build scripts
- Ensure test result formats work with Azure DevOps reporting
- Keep artifact publishing working correctly