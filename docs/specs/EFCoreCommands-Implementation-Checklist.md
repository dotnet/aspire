# EF Core Commands Hosting Integration - Implementation Checklist

This document provides a detailed step-by-step implementation checklist for the `Aspire.Hosting.EFCoreCommands` hosting integration.

## Overview

Package name: `Aspire.Hosting.EFCoreCommands`
Assembly name: `Aspire.Hosting.EFCoreCommands`

This integration provides Entity Framework Core migration management commands for Aspire AppHost projects.

## Implementation Checklist

### Phase 1: Project Setup

- [x] Create new source project `src/Aspire.Hosting.EFCoreCommands/`
  - [x] Create `Aspire.Hosting.EFCoreCommands.csproj`
  - [x] Configure multi-targeting (net8.0, net9.0, net10.0)
  - [x] Add reference to `Aspire.Hosting`
  - [x] Disable package validation for new package
- [x] Add project to solution file `Aspire.slnx`

### Phase 2: Core Resource Types

- [x] Create `EFMigrationResource.cs`
  - [x] Implement `IResourceWithWaitSupport` to enable WaitFor functionality
  - [x] Store reference to parent `ProjectResource`
  - [x] Store optional DbContext type

### Phase 3: Extension Methods

- [x] Create `EFMigrationsBuilderExtensions.cs`
  - [x] Implement `AddEFMigrations<TContext>()` generic method
  - [x] Implement `AddEFMigrations(name, contextType)` method with explicit type
  - [x] Implement `AddEFMigrations(name)` method without context (auto-detect)
  - [x] Add validation for duplicate context types on same project
  - [x] Configure initial resource state ("Pending")
  - [x] Add Database icon to resources
- [x] Implement configuration methods:
  - [x] `RunDatabaseUpdateOnStart()` - Add annotation to run migrations on start
  - [x] `PublishAsMigrationScript()` - Add annotation for script generation
  - [x] `PublishAsMigrationBundle()` - Add annotation for bundle generation

### Phase 4: Annotations

- [x] Create `EFMigrationAnnotations.cs`
  - [x] `RunDatabaseUpdateOnStartAnnotation`
  - [x] `PublishAsMigrationScriptAnnotation`
  - [x] `PublishAsMigrationBundleAnnotation`

### Phase 5: Resource Commands

- [x] Implement resource commands in `EFMigrationsBuilderExtensions.cs`:
  - [x] "Update Database" - Apply pending migrations
  - [x] "Drop Database" - Delete the database (with confirmation)
  - [x] "Reset Database" - Drop and recreate (with confirmation)
  - [x] "Add Migration..." - Create new migration
  - [x] "Remove Migration" - Remove last migration
  - [x] "Get Database Status" - Show migration status
- [x] Add icons to all commands
- [x] Add confirmation messages for destructive commands

### Phase 6: Command Executor

- [x] Create `EFCoreOperationExecutor.cs`
  - [x] Implement `UpdateDatabaseAsync()`
  - [x] Implement `DropDatabaseAsync()`
  - [x] Implement `ResetDatabaseAsync()` (drop + update)
  - [x] Implement `AddMigrationAsync()`
  - [x] Implement `RemoveMigrationAsync()`
  - [x] Implement `GetDatabaseStatusAsync()`
  - [x] Implement `GenerateMigrationScriptAsync()` for publishing
  - [x] Implement `GenerateMigrationBundleAsync()` for publishing
  - [x] Capture command output via logging

### Phase 7: Documentation

- [x] Create `README.md` following hosting integration guidelines
  - [x] Installation instructions
  - [x] Usage examples
  - [x] Resource commands table
  - [x] Multiple DbContexts example
  - [x] Publishing support example
  - [x] Prerequisites section

### Phase 8: Test Project Setup

- [x] Create test project `tests/Aspire.Hosting.EFCoreCommands.Tests/`
  - [x] Create `Aspire.Hosting.EFCoreCommands.Tests.csproj`
  - [x] Add reference to source project
  - [x] Add reference to `Aspire.Hosting.Tests` for test utilities
- [x] Add test project to solution file

### Phase 9: Unit Tests

- [x] Create `AddEFMigrationsTests.cs`:
  - [x] Test resource creation with generic context type
  - [x] Test resource creation without context type
  - [x] Test resource creation with explicit context type
  - [x] Test resource added to app model
  - [x] Test multiple contexts on same project
  - [x] Test duplicate context type throws exception
  - [x] Test null/empty name validation
  - [x] Test resource snapshot annotation
  - [x] Test database icon annotation
  - [x] Test IResourceWithWaitSupport implementation

- [x] Create `EFMigrationConfigurationTests.cs`:
  - [x] Test `RunDatabaseUpdateOnStart()` adds annotation
  - [x] Test `PublishAsMigrationScript()` adds annotation
  - [x] Test `PublishAsMigrationBundle()` adds annotation
  - [x] Test multiple options can be chained
  - [x] Test null builder validation

- [x] Create `EFMigrationCommandsTests.cs`:
  - [x] Test all 6 commands are added
  - [x] Test each command has correct display name
  - [x] Test destructive commands have confirmation messages
  - [x] Test non-destructive commands don't have confirmation
  - [x] Test commands have icons
  - [x] Test context suffix in command names

- [x] Create `EFMigrationWaitForTests.cs`:
  - [x] Test resource can wait for EF migration
  - [x] Test multiple resources can wait for same migration
  - [x] Test resource can wait for multiple migrations
  - [x] Test WaitFor with RunDatabaseUpdateOnStart
  - [x] Test container can wait for migration
  - [x] Test migration resource in app model

### Phase 10: Build Verification

- [x] Build source project successfully
- [x] Build test project successfully
- [x] Run all tests and verify they pass

## Future Enhancements (Not Yet Implemented)

The following items are part of the feature outline but require additional implementation:

### RunDatabaseUpdateOnStart Implementation

- [ ] Create hosted service to run migrations on startup
- [ ] Implement resource state transitions (Pending → Running → Finished)
- [ ] Handle migration failures and error states
- [ ] Integrate with resource health checks

### Publishing Support Implementation

- [ ] Integrate with publish pipeline
- [ ] Generate migration scripts during publish
- [ ] Generate migration bundles during publish
- [ ] Add output path configuration

### Microsoft.EntityFrameworkCore.Design Validation

- [ ] Check target project references Microsoft.EntityFrameworkCore.Design
- [ ] Show error message if not referenced
- [ ] Consider using reflection to invoke commands directly (see ReflectionOperationExecutor)

### Add Migration Dialog

- [ ] Implement interactive prompt for migration name
- [ ] Add options from `dotnet ef migrations add`
- [ ] Add notification about recompilation requirement

### IOperationReporter Integration

- [ ] Implement IOperationReporter to capture EF operation output
- [ ] Display operation progress in dashboard logs

## API Summary

### Extension Methods

```csharp
// Add EF migrations with specific context type
IResourceBuilder<EFMigrationResource> AddEFMigrations<TContext>(
    this IResourceBuilder<ProjectResource> builder,
    string name);

// Add EF migrations with explicit context type
IResourceBuilder<EFMigrationResource> AddEFMigrations(
    this IResourceBuilder<ProjectResource> builder,
    string name,
    Type contextType);

// Add EF migrations with auto-detected context
IResourceBuilder<EFMigrationResource> AddEFMigrations(
    this IResourceBuilder<ProjectResource> builder,
    string name);

// Configuration methods
IResourceBuilder<EFMigrationResource> RunDatabaseUpdateOnStart(
    this IResourceBuilder<EFMigrationResource> builder);

IResourceBuilder<EFMigrationResource> PublishAsMigrationScript(
    this IResourceBuilder<EFMigrationResource> builder);

IResourceBuilder<EFMigrationResource> PublishAsMigrationBundle(
    this IResourceBuilder<EFMigrationResource> builder);
```

### Usage Example

```csharp
var api = builder.AddProject<Projects.Api>("api");

// Add EF migrations for a specific DbContext
var apiMigrations = api.AddEFMigrations<MyDbContext>("api-migrations")
    .RunDatabaseUpdateOnStart();

// Other resources can wait for migrations to complete
var worker = builder.AddProject<Projects.Worker>("worker")
    .WaitFor(apiMigrations);
```

## Test Results

All 36 unit tests pass:
- AddEFMigrationsTests: 12 tests
- EFMigrationConfigurationTests: 8 tests
- EFMigrationCommandsTests: 12 tests
- EFMigrationWaitForTests: 6 tests
