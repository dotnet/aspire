# EF Core Commands Hosting Integration - Implementation Checklist

This document outlines the step-by-step implementation plan for the `Aspire.Hosting.EFCoreCommands` hosting integration package.

## Overview

The `Aspire.Hosting.EFCoreCommands` package provides EF migration management commands for Aspire-hosted projects. It enables developers to:

- Run EF migrations on AppHost start with dependency support via `WaitFor`
- Produce migration scripts and/or migration bundles as part of publishing
- Execute database migration commands through the Aspire Dashboard

## Implementation Checklist

### Phase 1: Project Setup

- [ ] Create `src/Aspire.Hosting.EFCoreCommands/` directory
- [ ] Create `Aspire.Hosting.EFCoreCommands.csproj` with:
  - Target frameworks: `$(AllTargetFrameworks)` for multi-targeting
  - Package tags: `aspire integration hosting ef efcore entityframeworkcore migrations database`
  - Description: Entity Framework Core migration management support for Aspire
  - ProjectReference to `Aspire.Hosting.csproj`
- [ ] Add project to `Aspire.slnx` solution file in the `/Hosting/` folder
- [ ] Create `README.md` following hosting integration guidelines

### Phase 2: Core Types and Annotations

- [ ] Create `EFMigrationResource.cs` - Resource representing EF migrations for a project
  - Properties: ContextType, ProjectResource reference, MigrationName
  - Implement `IResourceWithWaitSupport` for WaitFor functionality
- [ ] Create `EFMigrationsAnnotation.cs` - Annotation to track EF migration configuration
  - Store context type(s) associated with a project
  - Track RunOnStart configuration
- [ ] Create `EFMigrationStatusAnnotation.cs` - Track migration execution status

### Phase 3: Extension Methods

- [ ] Create `EFMigrationsBuilderExtensions.cs` with:
  - `AddEFMigrations<TContext>(this IResourceBuilder<ProjectResource>, string name)` - Add EF migrations for a specific DbContext type
  - `AddEFMigrations(this IResourceBuilder<ProjectResource>, string name, Type contextType)` - Non-generic overload
  - `AddEFMigrations(this IResourceBuilder<ProjectResource>, string name)` - Add migrations without specifying context (auto-detect)
  - Validation: Check for duplicate context types on same resource
  - Validation: Error if Microsoft.EntityFrameworkCore.Design is not referenced

- [ ] Create `EFMigrationsResourceBuilderExtensions.cs` with:
  - `RunDatabaseUpdateOnStart(this IResourceBuilder<EFMigrationResource>)` - Run migrations when AppHost starts
  - `PublishAsMigrationScript(this IResourceBuilder<EFMigrationResource>)` - Generate migration script during publish
  - `PublishAsMigrationBundle(this IResourceBuilder<EFMigrationResource>)` - Generate migration bundle during publish

### Phase 4: Resource Commands Implementation

- [ ] Create `EFMigrationCommands.cs` - Command implementations:
  - **Update Database Command**: Execute `dotnet ef database update` equivalent
  - **Drop Database Command**: Execute `dotnet ef database drop` equivalent (with confirmation)
  - **Reset Database Command**: Execute drop followed by update (with confirmation)
  - **Add Migration Command**: Prompt for migration name, execute `dotnet ef migrations add`
  - **Remove Migration Command**: Execute `dotnet ef migrations remove`
  - **Get Database Status Command**: Show current migration, pending migrations, pending model changes

- [ ] Create `EFMigrationCommandAnnotations.cs` - Add commands to resources:
  - Register all commands when `AddEFMigrations` is called
  - Use appropriate icons and confirmation messages
  - Implement `UpdateState` to enable/disable commands based on resource state

### Phase 5: EF Core Integration

- [ ] Create `EFCoreOperationExecutor.cs` - Wrapper around EF Core design-time operations:
  - Load Microsoft.EntityFrameworkCore.Design assembly via reflection
  - Implement IOperationReporter to capture output
  - Execute operations: UpdateDatabase, DropDatabase, AddMigration, RemoveMigration, GetMigrations, HasPendingModelChanges
  - Handle errors and report to resource logger

- [ ] Create `EFCoreDesignAssemblyValidator.cs` - Validate project setup:
  - Check if Microsoft.EntityFrameworkCore.Design is referenced
  - Report clear error messages if missing
  - Provide guidance on how to add the package

### Phase 6: Run on Start Implementation

- [ ] Create `EFMigrationStartupHook.cs` - Handle migrations at startup:
  - Implement as `AfterResourcesCreatedEvent` handler
  - Execute migrations for resources configured with `RunDatabaseUpdateOnStart`
  - Update resource state during migration execution
  - Support cancellation and error handling

- [ ] Integrate with WaitFor functionality:
  - Ensure resources waiting for migrations don't start until migrations complete
  - Properly handle migration failures (set FailedToStart state)
  - Log migration progress to resource logs

### Phase 7: Publishing Support

- [ ] Create `EFMigrationPublishingCallback.cs` - Handle publishing scenarios:
  - Generate migration scripts during `dotnet publish` for resources with `PublishAsMigrationScript`
  - Generate migration bundles for resources with `PublishAsMigrationBundle`
  - Add generated artifacts to publish output

### Phase 8: Tests

- [ ] Create `tests/Aspire.Hosting.EFCoreCommands.Tests/` directory
- [ ] Create `Aspire.Hosting.EFCoreCommands.Tests.csproj`
- [ ] Create test classes:
  - `AddEFMigrationsTests.cs` - Test extension method behavior
  - `EFMigrationResourceTests.cs` - Test resource creation and configuration
  - `EFMigrationCommandsTests.cs` - Test command annotations and state
  - `EFMigrationStartupTests.cs` - Test RunDatabaseUpdateOnStart with WaitFor
  - `EFMigrationValidationTests.cs` - Test design-time package validation

### Phase 9: Documentation and Finalization

- [ ] Update README.md with complete examples
- [ ] Add XML documentation to all public APIs
- [ ] Verify build passes
- [ ] Run all tests
- [ ] Run code review

## API Design

### Basic Usage

```csharp
var builder = DistributedApplication.CreateBuilder(args);

// Add a project with EF migrations
var api = builder.AddProject<Projects.Api>("api");

// Add EF migrations for a specific DbContext
var apiMigrations = api.AddEFMigrations<MyDbContext>("api-migrations");

// Configure to run on start with other resources waiting
apiMigrations.RunDatabaseUpdateOnStart();

// Other resources can wait for migrations to complete
var worker = builder.AddProject<Projects.Worker>("worker")
    .WaitFor(apiMigrations);

builder.Build().Run();
```

### Multiple DbContexts

```csharp
var api = builder.AddProject<Projects.Api>("api");

// Add migrations for multiple contexts
var userMigrations = api.AddEFMigrations<UserDbContext>("user-migrations");
var orderMigrations = api.AddEFMigrations<OrderDbContext>("order-migrations");

// Each will have their own set of commands in the dashboard
```

### Publishing

```csharp
var apiMigrations = api.AddEFMigrations<MyDbContext>("api-migrations")
    .PublishAsMigrationScript();  // Generate SQL script during publish
    // OR
    .PublishAsMigrationBundle();  // Generate migration bundle during publish
```

## Resource Commands (Dashboard)

When `AddEFMigrations` is called, the following commands will be available in the Aspire Dashboard:

| Command | Description | Confirmation Required |
|---------|-------------|----------------------|
| Update Database | Apply pending migrations | No |
| Drop Database | Delete the database | Yes |
| Reset Database | Drop and recreate with all migrations | Yes |
| Add Migration... | Create a new migration (prompts for name) | No |
| Remove Migration | Remove the last migration | No |
| Get Database Status | Show migration status | No |

## Notes

- The implementation uses EF Core's design-time APIs via reflection, similar to how `dotnet ef` works
- The target project must reference `Microsoft.EntityFrameworkCore.Design` for commands to work
- All commands are executed within the AppHost process, not via external process execution
- Migration status is tracked and displayed in the resource's state in the Dashboard
