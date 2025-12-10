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
  - [x] Implement `AddEFMigrations(name, contextTypeName)` method with context type as string
  - [x] Implement `AddEFMigrations(name)` method without context (auto-detect)
  - [x] Add validation for duplicate context types on same project
  - [x] Configure initial resource state ("Pending")
  - [x] Add Database icon to resources
- [x] Create `EFMigrationResourceBuilder` class (public, non-interface based)
  - [x] Stores context type name for use by configuration methods
  - [x] Implements `IResourceBuilder<EFMigrationResource>`
  - [x] Hide `ToString`, `Equals`, `GetHashCode` with `[EditorBrowsable]`
- [x] Implement configuration methods as instance methods on builder:
  - [x] `RunDatabaseUpdateOnStart()` - Set option on Resource.Options
  - [x] `PublishAsMigrationScript()` - Set option on Resource.Options
  - [x] `PublishAsMigrationBundle()` - Set option on Resource.Options

### Phase 4: Options

- [x] Create `EFMigrationsOptions` class
  - [x] `RunDatabaseUpdateOnStart` property
  - [x] `PublishAsMigrationScript` property  
  - [x] `PublishAsMigrationBundle` property
  - [x] Referenced directly from `EFMigrationResource.Options` (not as annotation)

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

- [x] Create `EFCoreOperationExecutor.cs` using `dotnet ef` CLI
  - [x] Uses dotnet ef CLI commands which internally uses OperationExecutor from target project's Microsoft.EntityFrameworkCore.Design reference
  - [x] Target project must reference Microsoft.EntityFrameworkCore.Design
  - [x] See: https://github.com/dotnet/efcore/blob/main/src/ef/ReflectionOperationExecutor.cs for how ef CLI invokes operations
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

## Future Enhancements (Partially Implemented)

The following items are part of the feature outline with implementation status:

### RunDatabaseUpdateOnStart Implementation

Implemented using `IDistributedApplicationEventingSubscriber` to hook into the application lifecycle events.

- [x] Create hosted service to run migrations on startup using `IDistributedApplicationEventingSubscriber` (`EFMigrationEventSubscriber`)
- [x] Implement resource state transitions (Pending → Running → Finished/FailedToStart)
- [x] Handle migration failures and error states
- [ ] Integrate with resource health checks

### Add Migration Dialog

Implemented using `IInteractionService.PromptInputAsync` to prompt for migration name:

```csharp
var interactionService = context.ServiceProvider.GetService<IInteractionService>();

// Fall back to auto-generated name if interaction service is not available
if (interactionService == null || !interactionService.IsAvailable)
{
    return $"Migration_{DateTime.UtcNow:yyyyMMddHHmmss}";
}

var result = await interactionService.PromptInputAsync(
    title: "Add Migration",
    message: "Enter the name for the new migration.",
    inputLabel: "Migration Name",
    placeHolder: "e.g. InitialCreate",
    cancellationToken: context.CancellationToken);

if (!result.Canceled && result.Data?.Value is { } migrationName)
{
    // Execute the migration with the provided name
}
```

- [x] Implement interactive prompt for migration name using `IInteractionService.PromptInputAsync`
- [x] Add notification about recompilation requirement (in dialog message and log output)
- [ ] Add options from `dotnet ef migrations add` (--output-dir, --namespace, etc.)

### Publishing Support Implementation

- [ ] Integrate with publish pipeline
- [ ] Generate migration scripts during publish
- [ ] Generate migration bundles during publish
- [ ] Add output path configuration

### Microsoft.EntityFrameworkCore.Design Validation

- [ ] Check target project references Microsoft.EntityFrameworkCore.Design
- [ ] Show error message if not referenced
- [ ] Consider using reflection to invoke commands directly (see [ReflectionOperationExecutor](https://github.com/dotnet/efcore/blob/main/src/ef/ReflectionOperationExecutor.cs))
  - Note: Directly referencing Microsoft.EntityFrameworkCore.Design causes package version conflicts with the Aspire build infrastructure

### IOperationReporter Integration

- [ ] Implement IOperationReporter to capture EF operation output
- [ ] Display operation progress in dashboard logs

## API Summary

### Custom Builder Class

```csharp
// Custom builder that stores the context type name (no interface)
public sealed class EFMigrationResourceBuilder : IResourceBuilder<EFMigrationResource>
{
    string? ContextTypeName { get; }
    
    // Configuration methods as instance methods
    EFMigrationResourceBuilder RunDatabaseUpdateOnStart();
    EFMigrationResourceBuilder PublishAsMigrationScript();
    EFMigrationResourceBuilder PublishAsMigrationBundle();
}
```

### Extension Methods

```csharp
// Add EF migrations with specific context type (generic)
EFMigrationResourceBuilder AddEFMigrations<TContext>(
    this IResourceBuilder<ProjectResource> builder,
    string name) where TContext : class;

// Add EF migrations with explicit context type
EFMigrationResourceBuilder AddEFMigrations(
    this IResourceBuilder<ProjectResource> builder,
    string name,
    Type contextType);

// Add EF migrations with context type name as string (runtime discovery)
EFMigrationResourceBuilder AddEFMigrations(
    this IResourceBuilder<ProjectResource> builder,
    string name,
    string contextTypeName);

// Add EF migrations with auto-detected context
EFMigrationResourceBuilder AddEFMigrations(
    this IResourceBuilder<ProjectResource> builder,
    string name);
```

### EFMigrationResource

```csharp
public class EFMigrationResource : Resource, IResourceWithWaitSupport
{
    ProjectResource ProjectResource { get; }
    Type? ContextType { get; }
    string? ContextTypeName { get; }
    EFMigrationsOptions Options { get; }  // Direct reference, not annotation
}
```

### Usage Example

```csharp
var api = builder.AddProject<Projects.Api>("api");

// Add EF migrations for a specific DbContext (compile-time type)
var apiMigrations = api.AddEFMigrations<MyDbContext>("api-migrations")
    .RunDatabaseUpdateOnStart();

// Or specify context type as a string (runtime discovery)
var otherMigrations = api.AddEFMigrations("other-migrations", "MyApp.Data.OtherDbContext");

// Other resources can wait for migrations to complete
var worker = builder.AddProject<Projects.Worker>("worker")
    .WaitFor(apiMigrations);
```

## Test Results

All 43 unit tests pass:
- AddEFMigrationsTests: 18 tests
- EFMigrationConfigurationTests: 8 tests
- EFMigrationCommandsTests: 12 tests
- EFMigrationWaitForTests: 6 tests
