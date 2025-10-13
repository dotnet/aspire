# Azure DevOps Matrix Testing Migration - Work Plan

**⚠️ IMPORTANT**: This targets **PUBLIC PIPELINE ONLY** (`dnceng-public/public`). Do NOT modify official/internal builds.

## Multi-Session Implementation Strategy

This work is designed to be completed across multiple coding sessions with different participants (human developers and AI assistants). The project is structured for **resumable development** with clear handoff points between sessions.

### Session Workflow Pattern
Each session follows a standard pattern to ensure continuity:

1. **Context Loading** (10-15 min): Review `AZDO_MATRIX_MIGRATION_SESSIONS.md` for current status
2. **Environment Setup** (5-10 min): Validate authentication and create/checkout feature branch
3. **Implementation** (60-90 min): Work on specific tasks from this work plan
4. **Testing & Validation** (30-60 min): Use automation scripts to validate changes
5. **Documentation** (10-15 min): Update tracking docs and plan next steps

### Handoff Requirements
- **Clear Status**: Each session updates task completion status
- **Branch Management**: Feature branches for work-in-progress
- **Issue Documentation**: Record any blockers or unexpected findings
- **Next Steps**: Explicit goals for the following session

## Phase 1: Integration Tests Matrix Implementation

### Prerequisites ✅
- [x] Analysis of current pipeline structure complete
- [x] Design document created
- [x] GitHub Actions test enumeration approach understood

### Task Breakdown

#### Task 1: Create Integration Tests Matrix Template
**File**: `eng/pipelines/templates/integration-tests-matrix.yml`
**Estimated Time**: 2-3 hours
**Session Assignment**: Session 2 (Primary focus)

**Subtasks**:
- [ ] **1.1**: Create new template file structure with parameters
- [ ] **1.2**: Add test enumeration step using `GetTestProjects.proj`
- [ ] **1.3**: Implement matrix generation logic for cross-platform jobs
- [ ] **1.4**: Configure job execution with platform-specific variables
- [ ] **1.5**: Set timeouts, resources, and artifact collection
- [ ] **1.6**: Add error handling and validation

**Dependencies**: None (can start immediately)
**Deliverable**: Working template that generates matrix jobs for integration tests

**Key Technical Details**:
- Use Azure DevOps `${{ each }}` syntax for matrix generation
- Support both Linux and Windows platforms
- Pass test short names to individual jobs
- Configure appropriate agent pools and images

#### Task 2: Modify Public Pipeline Template
**File**: `eng/pipelines/templates/public-pipeline-template.yml`
**Estimated Time**: 1-2 hours
**Session Assignment**: Session 2 (Secondary focus) or Session 3
**Dependencies**: Task 1 (integration-tests-matrix.yml must exist)

**Subtasks**:
- [ ] **2.1**: Add test enumeration step (reuse from GitHub Actions pattern)
- [ ] **2.2**: Update test variant logic to include `_integration_matrix_tests`
- [ ] **2.3**: Replace pipeline test jobs with matrix template call
- [ ] **2.4**: Disable Helix for integration tests, preserve for templates
- [ ] **2.5**: Validate no regression in template/EndToEnd test execution
- [ ] **2.6**: Update parameter passing and job dependencies

**Deliverable**: Modified template that routes integration tests to matrix jobs

**Key Changes**:
```yaml
# Current approach:
- job: Windows_pipeline_tests
  # runs ALL tests in single job

# New approach:
- template: /eng/pipelines/templates/integration-tests-matrix.yml
  parameters:
    platforms: ['Linux', 'Windows']
```

#### Task 3: Update Test Execution Logic
**Files**: Various template files
**Estimated Time**: 1-2 hours

**Subtasks**:
- [ ] Ensure individual test project execution works correctly
- [ ] Configure test filtering to match GitHub Actions (exclude quarantined/outerloop)
- [ ] Set up proper artifact collection for test results
- [ ] Configure timeout settings for individual test jobs
- [ ] Ensure Docker setup works for tests that need it

**Test Command Template**:
```bash
dotnet test tests/Aspire.{TestShortName}.Tests/Aspire.{TestShortName}.Tests.csproj \
  --configuration Release \
  --logger "trx;LogFileName={TestShortName}.trx" \
  --filter-not-trait "quarantined=true" --filter-not-trait "outerloop=true"
```

#### Task 4: Validate azdo-tests.yml Configuration
**File**: `eng/pipelines/azdo-tests.yml`
**Estimated Time**: 30 minutes

**Subtasks**:
- [ ] Verify the manual pipeline uses correct test variants
- [ ] Ensure it picks up the new matrix approach
- [ ] Test manual trigger functionality

**Expected Change**:
```yaml
# Current:
testVariants: '_pipeline_tests,_helix_tests'

# Updated (Phase 1):
testVariants: '_integration_matrix_tests,_helix_tests'
# (Helix still used for templates temporarily)
```

### Testing & Validation

#### Local Testing
- [ ] Validate YAML syntax using Azure DevOps extension or online tools
- [ ] Test GetTestProjects.proj execution locally
- [ ] Verify matrix generation logic produces expected job names

#### Pipeline Testing
- [ ] Run azdo-tests pipeline manually to validate changes
- [ ] Check that integration tests execute as separate matrix jobs
- [ ] Verify test results are properly collected and reported
- [ ] Compare test execution time vs current approach
- [ ] Ensure no test regressions vs GitHub Actions results

#### Monitoring Points
- [ ] Total pipeline execution time
- [ ] Individual job execution times
- [ ] Test pass/fail rates
- [ ] Resource usage (build agents)
- [ ] Artifact publishing success

### Risk Mitigation

#### Rollback Plan
- [ ] Keep original template logic commented out for easy rollback
- [ ] Create feature flag to switch between old and new approaches
- [ ] Test rollback procedure before going live

#### Failure Scenarios
- [ ] **Matrix generation fails**: Fallback to original approach
- [ ] **Individual jobs timeout**: Increase timeout or investigate specific tests
- [ ] **High agent usage**: Implement throttling or reduce parallel jobs
- [ ] **Test instability**: Compare results with GitHub Actions for validation

### Acceptance Criteria

#### Functional Requirements
- [ ] All integration tests execute as individual matrix jobs
- [ ] Both Linux and Windows platforms supported
- [ ] Test filtering matches GitHub Actions behavior
- [ ] Test results properly collected in Azure DevOps
- [ ] Manual pipeline trigger works correctly

#### Performance Requirements
- [ ] Total build time <= current approach (90 minutes)
- [ ] Individual job time <= 20 minutes
- [ ] No increase in test failure rates vs GitHub Actions

#### Quality Requirements
- [ ] YAML syntax validation passes
- [ ] No hard-coded values (use parameters/variables)
- [ ] Proper error handling and logging
- [ ] Clear job naming convention

## Phase 2 & 3 Planning (Future)

### Phase 2: Template Tests Matrix
**Estimated Timeline**: 1-2 weeks after Phase 1
- Migrate template tests to matrix approach
- Handle template-specific test execution requirements
- Integrate with package build dependencies

### Phase 3: EndToEnd Tests Integration
**Estimated Timeline**: 2-3 weeks after Phase 2
- Integrate EndToEnd tests into matrix (Linux only)
- Handle Docker and package dependencies
- Complete Helix removal

## Implementation Timeline

### Session-Based Implementation
**Note**: This work spans multiple sessions. Each session should focus on 1-2 tasks maximum.

**Session 1** (✅ Complete - 2025-10-13): Planning & Setup
- ✅ Created design documents and work plan
- ✅ Built automation tooling for Azure DevOps
- ✅ Updated configuration for public pipeline
- ✅ Verified all docs target dnceng-public correctly

**Session 2** (🎯 Ready - Next): Core Matrix Implementation
- **Primary Goal**: Create `integration-tests-matrix.yml` template (Task 1)
- **Secondary Goal**: Begin `public-pipeline-template.yml` modifications (Task 2.1-2.3)
- **Testing**: Initial validation with automation script
- **Duration**: 2-3 hours

**Session 3** (📋 Planned): Integration & Testing
- **Primary Goal**: Complete pipeline template modifications (Task 2.4-2.6)
- **Secondary Goal**: Debug matrix generation and test execution (Task 3)
- **Testing**: Full pipeline validation and performance testing
- **Duration**: 2-3 hours

**Session 4** (📋 Planned): Refinement & Validation
- **Primary Goal**: Fix any issues found in Session 3
- **Secondary Goal**: Performance optimization and documentation updates
- **Testing**: Comprehensive validation vs GitHub Actions results
- **Duration**: 1-2 hours

**Session 5+** (📋 Future): Phase 2 Preparation
- Template tests matrix planning
- Documentation cleanup
- Preparation for template test migration

## Success Metrics

### Immediate (Phase 1)
- Integration tests running in matrix jobs: **100%**
- Test execution time improvement: **>= 0% (no regression)**
- Test stability: **>= current GitHub Actions pass rate**

### Long-term (All Phases)
- Helix dependency elimination: **100%**
- Pipeline consistency with GitHub Actions: **100%**
- Build time improvement: **10-20%** (due to better parallelization)

## Notes & Assumptions

### Assumptions
- Build agent availability sufficient for increased parallel jobs
- Test projects are compatible with matrix execution approach
- Docker setup works correctly on Azure DevOps agents
- Test result collection works with current reporting tools

### Dependencies
- `GetTestProjects.proj` logic remains stable
- Azure DevOps YAML syntax supports required matrix operations
- Build agent pool configurations remain available