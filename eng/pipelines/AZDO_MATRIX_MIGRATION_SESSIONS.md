# Azure DevOps Matrix Migration - Session Tracking

## Project Overview

**Goal**: Migrate Azure DevOps testing from Helix-based execution to direct pipeline execution using matrix jobs, similar to GitHub Actions approach.

**Scope**: Public pipeline (`azure-pipelines-public.yml`) and manual test pipeline (`azdo-tests.yml`) only. **DO NOT modify** the official builds pipeline.

**Repository**:
- **Organization**: `dnceng-public`
- **Project**: `public`
- **Pipeline**: `azdo-tests` (manual trigger)

## Current Status: Planning & Setup Complete ✅

### Completed Work
- [x] **Design Document**: `/eng/pipelines/AZDO_MATRIX_MIGRATION_DESIGN.md`
- [x] **Work Plan**: `/eng/pipelines/AZDO_MATRIX_MIGRATION_WORKPLAN.md`
- [x] **Automation Script**: `/eng/scripts/azdo-pipeline-helper.sh`
- [x] **Automation Docs**: `/eng/scripts/README-AZDO-AUTOMATION.md`
- [x] **Configuration Update**: Updated for public pipeline (dnceng-public/public)

### Current Architecture Understanding
```
Current Public Pipeline:
├── Windows_pipeline_tests (ALL integration tests in single job - 90min timeout)
├── Linux_pipeline_tests (ALL integration tests in single job - 90min timeout)
├── Windows_helix_tests (sends ALL tests to Helix)
└── Linux_helix_tests (sends ALL tests to Helix)

Target Phase 1:
├── IntegrationMatrix_Linux_Dashboard (individual test - 20min timeout)
├── IntegrationMatrix_Linux_Hosting (individual test - 20min timeout)
├── IntegrationMatrix_Windows_Dashboard (individual test - 20min timeout)
├── IntegrationMatrix_Windows_Hosting (individual test - 20min timeout)
├── ... (matrix continues for all integration tests)
└── [Keep Helix for Templates temporarily]
```

## Implementation Phases

### Phase 1: Integration Tests Matrix 🎯 **NEXT**
**Scope**: Replace pipeline integration tests with matrix jobs

**Files to Modify**:
1. **NEW**: `eng/pipelines/templates/integration-tests-matrix.yml`
2. **MODIFY**: `eng/pipelines/templates/public-pipeline-template.yml`
3. **UPDATE**: Test variant from `_pipeline_tests` to `_integration_matrix_tests`

**Key Changes**:
- Eliminate `runPipelineTests` parameter completely
- Add test enumeration step using `tests/Shared/GetTestProjects.proj`
- Create matrix jobs for each integration test project on Linux + Windows
- Disable Helix for integration tests, keep for templates

### Phase 2: Template Tests Matrix (Future)
**Scope**: Migrate template tests from Helix to matrix

### Phase 3: EndToEnd Tests & Complete Helix Removal (Future)
**Scope**: Complete migration, remove all Helix dependencies

## Session Planning

### Session Requirements
- **Duration**: 2-3 hours for implementation + testing
- **Prerequisites**: Azure DevOps PAT token for `dnceng-public/public`
- **Branch Strategy**: Feature branches for each attempt
- **Testing**: Use `azdo-tests` pipeline for validation

### Standard Session Structure
Each implementation session should follow this pattern:

1. **Context Loading** (10-15 min)
   - Review this session tracking document
   - Check git status and any uncommitted work
   - Verify automation script authentication
   - Understand current state from previous sessions

2. **Environment Setup** (5-10 min)
   - Create or checkout feature branch
   - Verify build environment and dependencies
   - Test automation script connectivity

3. **Implementation** (60-90 min)
   - Make pipeline changes according to session goals
   - Validate YAML syntax locally where possible
   - Push changes to feature branch
   - Trigger initial test build

4. **Testing & Validation** (30-60 min)
   - Monitor build results using automation script
   - Analyze failures and debug issues
   - Iterate on fixes and retest
   - Compare results with expected behavior

5. **Session Wrap-up** (10-15 min)
   - Update this tracking document with progress
   - Document any issues encountered and solutions found
   - Update task status in work plan
   - Plan specific goals for next session
   - Clean up or preserve work-in-progress branch

## Session History

### Session 1: 2025-10-13 - Planning & Setup ✅
**Participants**: Human + Claude Code
**Duration**: ~2 hours
**Achievements**:
- Created comprehensive design and planning documents
- Built Azure DevOps automation tooling
- Updated configuration for public pipeline
- Ready to start implementation

**Key Decisions**:
- Focus on public pipeline only (not internal/official)
- Use dnceng-public organization for testing
- Start with integration tests only
- Eliminate pipeline tests completely (not just replace)

**Next Session Goals**:
- Implement integration tests matrix template
- Modify public pipeline template
- Test first working version

### Session 2: 2025-10-13 - Phase 1 Implementation
**Status**: ✅ **IMPLEMENTATION COMPLETE - READY FOR TESTING**
**Duration**: ~2 hours

**Goals**:
- [x] Create `integration-tests-matrix.yml` template
- [x] Modify `public-pipeline-template.yml`
- [x] Update test variants configuration
- [ ] Test on feature branch with automation script
- [ ] Debug and iterate until basic matrix works

**Pre-Session Checklist**:
- [ ] Verify AZDO_TOKEN environment variable is set
- [ ] Test automation script access: `./eng/scripts/azdo-pipeline-helper.sh validate`
- [ ] Create feature branch: `git checkout -b feature/matrix-session2`
- [ ] Review current Azure DevOps matrix syntax documentation

**Expected Challenges**:
- Azure DevOps matrix syntax differences from GitHub Actions
- Test enumeration integration
- Build artifact sharing between jobs
- Job naming conventions

**Success Criteria**:
- Integration tests run as separate matrix jobs
- Both Linux and Windows platforms work
- Test results properly collected
- Build time reasonable vs current approach

**Completed Work**:
- ✅ **Template Creation**: Built `integration-tests-matrix.yml` with dynamic test enumeration
- ✅ **Pipeline Integration**: Modified `public-pipeline-template.yml` to use `_integration_matrix_tests`
- ✅ **Test Variants**: Updated from `_pipeline_tests` to `_integration_matrix_tests`
- ✅ **Configuration**: Updated `azdo-tests.yml` to use new matrix approach
- ✅ **Branch Ready**: `feature/matrix-session2` with all changes committed

**Ready for Testing**: Branch contains working implementation that needs validation on Azure DevOps

## Key Technical Details

### Test Enumeration Logic (Reuse from GitHub Actions)
```bash
# Uses existing GetTestProjects.proj
dotnet build tests/Shared/GetTestProjects.proj \
  /p:TestsListOutputPath=artifacts/IntegrationTests.list

# Produces short names like:
# Dashboard
# Hosting
# Components.Tests
# etc.
```

### Matrix Job Template Structure
```yaml
# integration-tests-matrix.yml
parameters:
- name: platforms
  type: object
  default: ['Linux', 'Windows']

jobs:
- job: EnumerateTests
  # Generate test list and matrix

- job: RunIntegrationTests
  dependsOn: EnumerateTests
  strategy:
    matrix: $[ dependencies.EnumerateTests.outputs['GenerateMatrix.TestMatrix'] ]
  # Execute individual test projects
```

### Test Filtering (Match GitHub Actions)
```bash
--filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

## Automation Usage

### Setup (One Time)
```bash
# Get PAT token from https://dev.azure.com/dnceng-public
export AZDO_TOKEN="your_token_here"
./eng/scripts/azdo-pipeline-helper.sh setup --token "$AZDO_TOKEN"
```

### Testing Workflow
```bash
# 1. Create branch and push changes
git checkout -b feature/matrix-migration-session2
# ... make changes ...
git add . && git commit -m "Session 2: Implement integration matrix" && git push

# 2. Trigger and monitor build
BUILD_ID=$(./eng/scripts/azdo-pipeline-helper.sh trigger --branch feature/matrix-migration-session2 --wait)

# 3. Check results
./eng/scripts/azdo-pipeline-helper.sh results --build-id $BUILD_ID

# 4. Debug if needed
./eng/scripts/azdo-pipeline-helper.sh logs --build-id $BUILD_ID
```

## Risk Management

### Session Continuity Risks
- **Context Loss**: Detailed session tracking document mitigates
- **Code Conflicts**: Feature branch strategy prevents conflicts
- **Incomplete State**: Clear status tracking for each session

### Technical Risks
- **Matrix Generation**: Complex Azure DevOps syntax
- **Build Performance**: Many parallel jobs may strain resources
- **Test Instability**: Matrix isolation might reveal hidden dependencies

### Mitigation Strategies
- **Incremental Approach**: One change at a time, test frequently
- **Rollback Plan**: Keep original code commented for quick revert
- **Documentation**: Detailed tracking of what works/doesn't work

## Success Metrics

### Phase 1 Success
- [ ] Integration tests execute as individual matrix jobs
- [ ] Both Linux and Windows platforms supported
- [ ] Test results collected properly
- [ ] Build time <= current approach (90 minutes)
- [ ] Test pass rate >= GitHub Actions equivalents

### Long-term Success
- [ ] Complete elimination of Helix dependency
- [ ] Faster feedback for test failures
- [ ] Consistent test behavior with GitHub Actions
- [ ] Reduced resource usage vs Helix queuing

## Notes for Future Sessions

### Context Loading Checklist
1. Read this session tracking document
2. Check current branch status and any in-progress work
3. Review any error messages or build failures from previous session
4. Verify automation script still works with current authentication

### Common Debugging Patterns
- **YAML Syntax**: Use Azure DevOps extension or online validators
- **Matrix Generation**: Test PowerShell/Bash logic separately
- **Test Enumeration**: Verify `GetTestProjects.proj` works locally
- **Build Failures**: Compare with working GitHub Actions implementation

### Useful Reference Links
- **Pipeline**: https://dev.azure.com/dnceng-public/public/_build?definitionId=[TBD]
- **GitHub Actions Reference**: `.github/workflows/tests.yml`
- **Test Enumeration**: `tests/Shared/GetTestProjects.proj`
- **Current Template**: `eng/pipelines/templates/public-pipeline-template.yml`