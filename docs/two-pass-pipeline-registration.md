# Two-Pass Pipeline Step Registration

## Overview

The pipeline step system now uses a **two-pass registration approach** that eliminates ordering constraints when defining dependencies between steps.

## How It Works

### Pass 1: Registration
All `DeployingCallbackAnnotation` callbacks are invoked and their `PipelineStep` objects are registered in the `PipelineStepRegistry`. This happens in the order resources are added to the application model.

### Pass 2: Resolution
After all steps are registered, each step's `DependsOn` callbacks are evaluated with access to the complete registry. Dependencies are resolved and stored in each step's `Dependencies` list.

## Key Components

### 1. PipelineStep
Updated to support lazy dependency resolution:
- `DependsOn(Func<PipelineStepRegistry, IEnumerable<string>>)` - Callback-based resolution
- `DependsOn(string)` - Simple string-based dependency
- `Dependencies` - Read-only property populated after resolution

### 2. PipelineStepRegistry
Thread-safe registry for discovering steps:
- `Register(step)` - Register a step
- `TryGetStep(name)` - Lookup by name
- `GetAllSteps()` - Get all registered steps
- `GetAllStepNames()` - Get all registered step names
- `HasStep(name)` - Check if a step exists

### 3. DeployingContext
Provides access to the registry:
- `StepRegistry` property - Access the shared registry
- `WriteModelAsync()` - Implements two-pass registration

### 4. Extension Methods
Convenient helpers for common scenarios:
- `DependsOnStep(name, required)` - Lookup step by name
- `DependsOnFirst(predicate)` - Depend on first matching step
- `DependsOnAll(predicate)` - Depend on all matching steps

### 5. WellKnownPipelineSteps
Constants for framework-provided steps:
- `ProvisionBicepResources`
- `ProvisionContainerApps`
- `ProvisionAppService`
- `BuildContainerImages`
- `PushContainerImages`

### 6. PipelineContext
Shared context for passing data between pipeline steps:
- `SetOutput(key, value)` - Store an output value from a step
- `GetOutput(key)` - Retrieve an output value (throws if not found)
- `GetOutput<T>(key)` - Retrieve a typed output value
- `TryGetOutput(key, out value)` - Try to retrieve an output value
- `TryGetOutput<T>(key, out value)` - Try to retrieve a typed output value
- `HasOutput(key)` - Check if an output exists
- `Outputs` - Read-only dictionary of all outputs

## Benefits

✅ **Order-Independent**: Steps can reference each other regardless of registration order
✅ **Type-Safe Discovery**: Use predicates instead of magic strings
✅ **Cross-Library Support**: Steps from different packages can discover each other
✅ **Flexible**: Support for required/optional, single/multiple, and conditional dependencies
✅ **Backward Compatible**: Simple string-based dependencies still work

## Usage Patterns

### Simple String Dependency
```csharp
step.DependsOn(WellKnownPipelineSteps.ProvisionBicepResources);
```

### Registry Lookup
```csharp
step.DependsOnStep("BuildStaticSite");
```

### Conditional Dependencies
```csharp
step.DependsOnFirst(s => s.Name.Contains("Deploy"));
step.DependsOnAll(s => s.Name.StartsWith("Build"));
```

### Callback-Based Resolution
```csharp
step.DependsOn(registry =>
{
    if (registry.TryGetStep("OptionalStep", out var optional))
    {
        return [optional.Name];
    }
    return [];
});
```

### Fluent Chaining
```csharp
```csharp
step.DependsOn(WellKnownPipelineSteps.ProvisionBicepResources)
    .DependsOnStep("BuildStaticSite")
    .DependsOnFirst(s => s.Name.EndsWith("Config"));
```

## Example Scenarios

### Scenario 1: Library Providing Reusable Steps
```csharp
// In a NuGet package: MyCompany.Aspire.BuildSteps
public static class BuildStepExtensions
{
    public static IResourceBuilder<T> WithTypescriptBuild<T>(
        this IResourceBuilder<T> builder)
        where T : IResourceWithAnnotations
    {
        return builder.WithAnnotation(new DeployingCallbackAnnotation(context =>
        {
            var step = new PipelineStep
            {
                Name = "BuildTypeScript",
                Action = async (ctx, pipelineContext) => { /* build TS */ }
            };

            // Register for discovery
            context.StepRegistry.Register(step);

            return step;
        }));
    }
}
```

### Scenario 2: Consumer Depending on Library Steps
```csharp
// In user's AppHost
builder.AddNodeApp("my-app", "./src")
    .WithTypescriptBuild() // From library
    .WithAnnotation(new DeployingCallbackAnnotation(context =>
    {
        var step = new PipelineStep
        {
            Name = "DeployApp",
            Action = async (ctx, pipelineContext) => { /* deploy */ }
        };

        // Depend on the TypeScript build step
        step.DependsOnStep("BuildTypeScript");

        return step;
    }));
```

### Scenario 3: Optional Dependencies
```csharp
var step = new PipelineStep
{
    Name = "DeployWithCache",
    Action = async (ctx) => { /* deploy */ }
};

// Deploy depends on cache warmup if it exists, but doesn't require it
step.DependsOnStep("WarmupCache", required: false);
```

## Architecture

```text
┌─────────────────────────────────────────────┐
│         DeployingContext                    │
│                                             │
│  ┌──────────────────────────────────────┐  │
│  │     PipelineStepRegistry             │  │
│  │  - Thread-safe storage               │  │
│  │  - Name-based lookup                 │  │
│  └──────────────────────────────────────┘  │
└─────────────────────────────────────────────┘
                    │
                    │ Shared across all callbacks
                    ▼
┌─────────────────────────────────────────────┐
│          WriteModelAsync()                  │
│                                             │
│  PASS 1: Registration                       │
│  ┌────────────────────────────────────┐    │
│  │  foreach resource/annotation       │    │
│  │    step = callback(context)        │    │
│  │    registry.Register(step)         │    │
│  └────────────────────────────────────┘    │
│                                             │
│  PASS 2: Resolution                         │
│  ┌────────────────────────────────────┐    │
│  │  foreach step                      │    │
│  │    step.ResolveDependencies(reg)   │    │
│  │    pipeline.AddStep(step)          │    │
│  └────────────────────────────────────┘    │
│                                             │
│  EXECUTION:                                 │
│  ┌────────────────────────────────────┐    │
│  │  pipeline.ExecuteAsync()           │    │
│  │  (topological sort + execute)      │    │
│  └────────────────────────────────────┘    │
└─────────────────────────────────────────────┘
```

## Migration Guide

### Before (Order-Dependent)
```csharp
step.Dependencies.Add("BuildStaticSite"); // Had to be registered already
```

### After (Order-Independent)
```csharp
// Option 1: Still works, but now order-independent
step.DependsOn("BuildStaticSite");

// Option 2: Type-safe lookup
step.DependsOnStep("BuildStaticSite");

// Option 3: Predicate-based discovery
step.DependsOnFirst(s => s.Name.StartsWith("Build"));

// Option 4: Custom resolution
step.DependsOn(registry =>
{
    var steps = registry.GetAllSteps().Where(s => s.Name.Contains("Infrastructure"));
    return steps.Select(s => s.Name);
});
```

## Testing

The two-pass approach makes testing easier:
1. Register all steps in any order
2. Call `ResolveDependencies()` on each step
3. Verify the `Dependencies` property contains expected values

```csharp
var registry = new PipelineStepRegistry();
var step1 = new PipelineStep { Name = "Step1" };
var step2 = new PipelineStep { Name = "Step2" };

registry.Register(step1);
registry.Register(step2);

step2.DependsOnStep("Step1");
step2.ResolveDependencies(registry);

Assert.Contains("Step1", step2.Dependencies);
```

## Passing Data Between Steps

Pipeline steps can share data using the `PipelineContext` which is passed to each step's `Action` along with the `DeployingContext`.

### Storing Outputs

A step can store outputs for consumption by downstream steps:

```csharp
var step = new PipelineStep
{
    Name = "BuildStaticSite",
    Action = async (deployingContext, pipelineContext) =>
    {
        // Build the static site...
        var distPath = Path.Combine(staticSitePath, "dist");

        // Store the output for other steps to use
        pipelineContext.SetOutput("BuildStaticSite:distPath", distPath);
    }
};
```

### Consuming Inputs

Downstream steps can retrieve outputs from previous steps:

```csharp
var step = new PipelineStep
{
    Name = "PushStaticSite",
    Action = async (deployingContext, pipelineContext) =>
    {
        // Retrieve the distPath from BuildStaticSite step
        if (!pipelineContext.TryGetOutput<string>("BuildStaticSite:distPath", out var distPath))
        {
            throw new InvalidOperationException("BuildStaticSite step did not produce a distPath output");
        }

        // Use the distPath to upload files...
        var files = Directory.GetFiles(distPath, "*", SearchOption.AllDirectories);
        // ...
    }
};
step.DependsOn("BuildStaticSite"); // Ensure BuildStaticSite runs first
```

### Naming Convention

Output keys typically follow the pattern `StepName:OutputName`:
- `BuildStaticSite:distPath` - The dist directory path from BuildStaticSite
- `ProvisionBicepResources:storageAccountName` - Storage account name from provisioning
- `BuildContainerImages:imageTag` - Container image tag from build

### Type Safety

Use generic overloads for type-safe access:

```csharp
// Throws if not found or wrong type
var distPath = pipelineContext.GetOutput<string>("BuildStaticSite:distPath");

// Safe access with TryGetOutput
if (pipelineContext.TryGetOutput<Dictionary<string, string>>("Deploy:outputs", out var outputs))
{
    // Use outputs...
}

// Check existence
if (pipelineContext.HasOutput("BuildStaticSite:distPath"))
{
    // Output exists
}
```

### Thread Safety

`PipelineContext` uses a `ConcurrentDictionary` internally, making it safe to access from multiple steps executing concurrently (though the current implementation executes steps sequentially in dependency order).

