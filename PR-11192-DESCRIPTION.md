# Add QuarantineTool for Managing Flaky Tests

## Overview

This PR introduces a new **QuarantineTool** - a Roslyn-based command-line utility that helps developers efficiently quarantine or unquarantine failing/flaky xUnit tests by automatically adding or removing the `[QuarantinedTest]` attribute on test methods across the repository.

## Why This Tool Is Needed

Managing flaky tests in a large codebase like Aspire can be challenging. When tests become unreliable, they need to be temporarily quarantined to prevent CI failures while the underlying issues are being investigated and fixed. Previously, this process required:

- Manually finding and editing test files
- Manually adding/removing `[QuarantinedTest]` attributes
- Manually managing `using` directives
- Risk of human error in the editing process

The QuarantineTool automates this entire workflow, making it safer and more efficient.

## Key Features

### 🎯 **Precise Test Targeting**
- Supports fully-qualified test method names: `Namespace.Type.Method`
- Handles nested types with `+` notation: `Namespace.Outer+Inner.TestMethod`
- Matches tests by their complete namespace and type hierarchy

### 🔧 **Safe Code Modifications**
- Uses Roslyn (Microsoft.CodeAnalysis) for safe, structured source code modifications
- Preserves existing code formatting and indentation
- Respects file encoding and line ending conventions
- Atomic operations - either succeeds completely or makes no changes

### 🚀 **Intelligent Automation**
- Automatically adds/removes `using Aspire.TestUtilities;` directives as needed
- Idempotent operations - safe to run multiple times
- Parallel processing for performance on large codebases
- Smart filtering to avoid parsing irrelevant files

### 📋 **Two Main Operations**

#### Quarantine Tests
```bash
dotnet run --project tools/QuarantineTools -- -q Full.Namespace.Type.Method -i https://github.com/dotnet/aspire/issues/1234
```
- Adds `[QuarantinedTest("issue-url")]` attribute to the specified test method
- Requires an issue URL for tracking purposes
- Automatically adds `using Aspire.TestUtilities;` if not present

#### Unquarantine Tests
```bash
dotnet run --project tools/QuarantineTools -- -u Full.Namespace.Type.Method
```
- Removes `[QuarantinedTest]` attribute from the specified test method
- Automatically removes `using Aspire.TestUtilities;` if no other quarantined tests remain in the file

## Usage Examples

### Basic Operations
```bash
# Show help
dotnet run --project tools/QuarantineTools -- --help

# Quarantine a single test
dotnet run --project tools/QuarantineTools -- -q Aspire.Hosting.Tests.ContainerResourceTests.ContainerWithArgsTest -i https://github.com/dotnet/aspire/issues/1234

# Unquarantine a test
dotnet run --project tools/QuarantineTools -- -u Aspire.Hosting.Tests.ContainerResourceTests.ContainerWithArgsTest

# Handle nested types
dotnet run --project tools/QuarantineTools -- -q Namespace.OuterClass+InnerClass.TestMethod -i https://github.com/dotnet/aspire/issues/5678
```

### Advanced Options
```bash
# Custom tests root folder
dotnet run --project tools/QuarantineTools -- -q -r tests/Aspire.Hosting.Tests -i https://github.com/dotnet/aspire/issues/1 My.Test.Method

# Custom attribute (for other projects)
dotnet run --project tools/QuarantineTools -- -q -a MyCompany.Testing.SkipTest -i https://example.com/issue/1 My.Test.Method
```

## Files Added/Modified

### New Tool Implementation
- **`tools/QuarantineTools/Quarantine.cs`** (773 lines) - Main tool implementation with comprehensive documentation
- **`tools/QuarantineTools/Quarantine.csproj`** - Project file targeting .NET 10 with AOT publishing
- **`tools/QuarantineTools/README.md`** - Detailed usage documentation
- **`tools/QuarantineTools/Directory.Build.props`** - Minimal build configuration
- **`tools/QuarantineTools/Directory.Build.targets`** - Minimal build configuration  
- **`tools/QuarantineTools/Directory.Packages.props`** - Package versions for Roslyn and System.CommandLine

### Comprehensive Test Suite
- **`tests/QuarantineTools.Tests/QuarantineTools.Tests.csproj`** - Test project
- **`tests/QuarantineTools.Tests/CliParsingBehaviorTests.cs`** (144 lines) - CLI argument parsing validation
- **`tests/QuarantineTools.Tests/QuarantineScriptTests.cs`** (466 lines) - Core functionality tests including:
  - Adding/removing attributes correctly
  - Handling various namespace and type configurations
  - Managing using directives
  - Preserving code formatting
  - URL validation
  - Idempotent operations

### Repository Integration
- **`Aspire.slnx`** - Added new projects to solution
- **`Directory.Packages.props`** - Added Microsoft.CodeAnalysis.CSharp package reference
- **`tests/Directory.Build.targets`** - Added test output capture configuration for CI
- **`.github/copilot-instructions.md`** - Updated with tool usage information
- **`.github/instructions/quarantine.instructions.md`** - GitHub-specific usage instructions

## Integration with Existing Workflows

### CI/Testing Integration
- The tool integrates with existing quarantined test infrastructure using `[QuarantinedTest]` attributes
- Maintains compatibility with current test filtering in CI pipelines
- Supports the existing pattern of excluding quarantined tests from regular runs

### Developer Workflow
- Simple command-line interface for quick test quarantining
- Follows repository conventions for tool placement and structure
- Provides clear feedback on what files were modified

### GitHub Integration
- GitHub-specific instructions for Copilot integration
- Tool reference added to copilot instructions for automated assistance

## Technical Implementation Details

### Architecture
- Built on .NET 10 with native AOT compilation for fast startup
- Uses Microsoft.CodeAnalysis.CSharp for robust syntax tree manipulation
- Leverages System.CommandLine for modern CLI experience
- Parallel processing with configurable concurrency

### Safety Features
- Comprehensive validation of input arguments
- Pre-filtering to avoid parsing irrelevant files
- Atomic file operations with proper error handling
- Preserves file encoding and line endings
- Extensive test coverage for edge cases

### Performance Optimizations
- Text-based pre-filtering before Roslyn parsing
- Parallel file processing
- Smart directory traversal avoiding build artifacts
- Efficient syntax tree manipulation

## Quality Assurance

This PR includes comprehensive testing:
- **CLI parsing validation** - Ensures correct argument handling
- **Core functionality tests** - Validates all major operations
- **Edge case handling** - Tests various code patterns and scenarios
- **Integration validation** - Confirms tool works with real-world code patterns

All tests follow repository conventions and integrate with the existing test infrastructure.

## Future Enhancements

The tool is designed to be extensible:
- Could support batch operations from configuration files
- Could integrate with issue tracking systems
- Could provide reporting on quarantined test statistics
- Could support additional attribute types for other scenarios

---

This tool significantly improves the developer experience for managing flaky tests in the Aspire repository, reducing manual effort and potential for errors while maintaining code quality and CI reliability.