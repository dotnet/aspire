# TestEnumerationRunsheetBuilder Design

## Overview

This document describes the design for migrating the current `GetTestProjects.proj` test enumeration mechanism to work through the Arcade SDK's runsheet builder pattern.

## Current Architecture

### GetTestProjects.proj (Current)
- **Approach**: Centralized test project discovery and enumeration
- **Invocation**: Manual execution via `dotnet build tests/Shared/GetTestProjects.proj`
- **Process**:
  1. Discovers all test projects using glob patterns
  2. Calls MSBuild on each project to determine GitHub Actions eligibility
  3. Builds split test projects to generate test class lists
  4. Generates final test lists and matrices using PowerShell scripts

### Problems with Current Approach
- Requires explicit invocation outside the standard build process
- Not integrated with Arcade SDK's runsheet builder mechanism
- Duplicates logic that could be shared with other runsheet builders

## New Architecture: TestEnumerationRunsheetBuilder

### Design Principles
1. **Distributed Processing**: Each test project generates its own enumeration data during build
2. **Arcade SDK Integration**: Follows the same pattern as existing runsheet builders
3. **Reuse Existing Logic**: Leverages existing test enumeration and splitting mechanisms
4. **Centralized Combination**: Final aggregation happens in `AfterSolutionBuild.targets`

### Components

#### 1. TestEnumerationRunsheetBuilder.targets
- **Location**: `eng/TestEnumerationRunsheetBuilder/TestEnumerationRunsheetBuilder.targets`
- **Purpose**: Runs once per test project to generate test enumeration data
- **Outputs**: Per-project test enumeration files in `ArtifactsTmpDir`

#### 2. Enhanced AfterSolutionBuild.targets
- **Purpose**: Combines individual test enumeration files into final outputs
- **Trigger**: When `TestRunnerName=TestEnumerationRunsheetBuilder`
- **Outputs**: Same as current GetTestProjects.proj (test lists and matrices)

### Flow Diagram

```
Build Process
├── For each test project:
│   ├── TestEnumerationRunsheetBuilder.targets runs
│   ├── Generates project-specific enumeration data
│   └── Writes to artifacts/tmp/{project}.testenumeration.json
│
└── After all projects built:
    ├── AfterSolutionBuild.targets runs
    ├── Collects all testenumeration.json files
    ├── Processes split tests (if any)
    └── Generates final outputs:
        ├── TestsForGithubActions.list
        ├── TestsForGithubActions.list.split-projects
        └── test-matrices/*.json
```

## Implementation Details

### TestEnumerationRunsheetBuilder.targets

```msbuild
<Target Name="RunTests"
        Condition="'$(SkipTests)' != 'true' and '$(IsGitHubActionsRunner)' == 'true' and '$(RunOnGithubActions)' == 'true'">

  <!-- Reuse existing GetRunTestsOnGithubActions target from Testing.targets -->
  <MSBuild Projects="$(MSBuildProjectFullPath)"
           Targets="GetRunTestsOnGithubActions"
           Properties="BuildOs=$(BuildOs);PrepareForHelix=true">
    <Output TaskParameter="TargetOutputs" ItemName="_ProjectInfo" />
  </MSBuild>

  <!-- Generate enumeration data for this project -->
  <ItemGroup>
    <_EnumerationData Include="{
      'project': '$(MSBuildProjectName)',
      'fullPath': '$(MSBuildProjectFullPath)',
      'shortName': '$(_ShortName)',
      'runOnGithubActions': '%(_ProjectInfo.RunTestsOnGithubActions)',
      'splitTests': '%(_ProjectInfo.SplitTests)'
    }" />
  </ItemGroup>

  <!-- Write to per-project enumeration file -->
  <WriteLinesToFile File="$(ArtifactsTmpDir)/$(MSBuildProjectName).testenumeration.json"
                    Lines="@(_EnumerationData)"
                    Overwrite="true" />
</Target>
```

### Enhanced AfterSolutionBuild.targets

```msbuild
<Target Name="_GenerateTestEnumeration"
        BeforeTargets="Test"
        Condition="'$(TestRunnerName)' == 'TestEnumerationRunsheetBuilder'">

  <!-- Collect all test enumeration files -->
  <ItemGroup>
    <_TestEnumerationFiles Include="$(ArtifactsTmpDir)/*.testenumeration.json" />
  </ItemGroup>

  <!-- Process enumeration files to generate final outputs -->
  <PropertyGroup>
    <_ProcessingScript>
      # PowerShell script to:
      # 1. Read all testenumeration.json files
      # 2. Filter by OS and eligibility
      # 3. Generate test lists and split test lists
      # 4. Call existing matrix generation script for split tests
    </_ProcessingScript>
  </PropertyGroup>

  <!-- Execute processing script -->
  <Exec Command="pwsh -Command '$(_ProcessingScript)'" />
</Target>
```

## Migration Strategy

### Phase 1: Implementation
1. Create `TestEnumerationRunsheetBuilder.targets`
2. Enhance `AfterSolutionBuild.targets` with test enumeration logic
3. Implement PowerShell processing script

### Phase 2: Integration
1. Update GitHub Actions workflows to use new approach
2. Test compatibility with existing split test functionality
3. Validate output format matches current GetTestProjects.proj

### Phase 3: Cleanup
1. Deprecate GetTestProjects.proj usage in workflows
2. Remove manual invocation commands
3. Document new usage pattern

## Usage

### Command Line
```bash
# Instead of manual GetTestProjects.proj invocation:
dotnet build tests/Shared/GetTestProjects.proj /bl:artifacts/log/Debug/GetTestProjects.binlog /p:TestsListOutputPath=artifacts/TestsForGithubActions.list /p:TestMatrixOutputPath=artifacts/test-matrices/ /p:ContinuousIntegrationBuild=true /p:BuildOs=linux

# New approach using runsheet builder:
./build.cmd -test /p:TestRunnerName=TestEnumerationRunsheetBuilder /p:TestsListOutputPath=artifacts/TestsForGithubActions.list /p:TestMatrixOutputPath=artifacts/test-matrices/ /p:ContinuousIntegrationBuild=true /p:BuildOs=linux
```

### Integration with CI
The new approach integrates seamlessly with the existing build infrastructure and requires minimal changes to GitHub Actions workflows.

## Benefits

1. **Consistency**: Follows the same pattern as other runsheet builders
2. **Automatic Discovery**: No manual project enumeration required
3. **Build Integration**: Leverages existing build process and caching
4. **Maintainability**: Reduces code duplication and improves consistency
5. **Extensibility**: Easy to add new test enumeration features

## Backward Compatibility

- Existing GetTestProjects.proj functionality remains unchanged
- New approach generates identical output formats
- Migration can be done incrementally per workflow