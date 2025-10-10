---
applyTo: "src/**/*.cs"
---

# XML Documentation Guidelines for Aspire

## Overview

All public .NET APIs in Aspire must have comprehensive XML documentation comments. Good XML documentation should be clear, concise, and provide practical guidance to developers using the APIs.

## Quality Standards

A high-quality XML documentation comment includes:

- **Concise summary**: Clear, brief description of what the API does  
- **Comprehensive remarks**: Detailed explanation of behavior, usage patterns, and important notes  
- **Complete parameter documentation**: All parameters documented with clear descriptions  
- **Code samples**: Practical examples showing realistic usage scenarios  
- **Exception documentation**: Document exceptions that can be thrown  
- **Return value documentation**: Clear description of what is returned  
- **Cross-references**: Use `<see cref=""/>` for related types and methods

## Documentation Templates by Type

### Classes

```csharp
/// <summary>
/// Provides extension methods for adding [resource type] resources to the distributed application.
/// </summary>
/// <remarks>
/// <para>
/// This class contains methods for configuring [resource type] within an Aspire application.
/// [Additional context about the resource type, when to use it, etc.]
/// </para>
/// <para>
/// [Any important notes about usage patterns, limitations, or dependencies]
/// </para>
/// </remarks>
public static class ResourceBuilderExtensions
{
}
```

### Interfaces

```csharp
/// <summary>
/// Represents a [brief description of what the interface represents].
/// </summary>
/// <remarks>
/// <para>
/// [Detailed explanation of the interface's purpose and role in the system]
/// </para>
/// <para>
/// [Usage guidance, implementation notes, or important considerations]
/// </para>
/// <example>
/// [Show a typical implementation or usage pattern]
/// <code lang="csharp">
/// // Example implementation or usage
/// </code>
/// </example>
/// </remarks>
public interface IExampleInterface
{
}
```

### Methods (Extension Methods)

```csharp
/// <summary>
/// [Brief description of what the method does - use active voice, start with a verb]
/// </summary>
/// <typeparam name="T">[Description of generic type parameter and any constraints]</typeparam>
/// <param name="builder">[Standard description for IDistributedApplicationBuilder]</param>
/// <param name="name">[Description of the resource name parameter]</param>
/// <param name="otherParam">[Clear description of what this parameter controls]</param>
/// <returns>[Description of what is returned and how it can be used]</returns>
/// <exception cref="ArgumentNullException">Thrown when [parameter] is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when [specific condition that causes this].</exception>
/// <remarks>
/// <para>
/// [Detailed explanation of the method's behavior, when to use it, and any important considerations]
/// </para>
/// <para>
/// [Additional context about configuration options, dependencies, or integration patterns]
/// </para>
/// <example>
/// [Realistic example showing the method in context with other Aspire APIs]
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
/// 
/// // [Brief explanation of what this example demonstrates]
/// var resource = builder.AddResource("name", configuration)
///                      .WithConfiguration(options);
/// 
/// builder.Build().Run();
/// </code>
/// </example>
/// </remarks>
public static IResourceBuilder<T> AddResource<T>(this IDistributedApplicationBuilder builder, string name, SomeConfig config)
{
}
```

### Properties

```csharp
/// <summary>
/// Gets or sets [brief description of what the property represents].
/// </summary>
/// <value>
/// [Description of the property value, including default values, valid ranges, or constraints]
/// </value>
/// <remarks>
/// [Additional context about when this property is used, side effects, or important notes]
/// </remarks>
public string PropertyName { get; set; }
```

## Specific Guidelines for Aspire APIs

### Extension Methods for IDistributedApplicationBuilder

**Standard parameter descriptions:**
- `builder`: The <see cref="IDistributedApplicationBuilder"/>.
- `name`: The name of the resource. This name will be used for service discovery when referenced in a dependency.

**Common return descriptions:**
- `A reference to the <see cref="IResourceBuilder{T}"/>.`

### Security-Related Parameters

Always document security implications:

```csharp
/// <param name="secret">A flag indicating whether the parameter should be regarded as secret. 
/// Secret parameters are not displayed in plain text in the dashboard and are handled securely.</param>
```

### Resource Configuration Methods

Include practical examples showing chaining:

```csharp
/// <example>
/// <code lang="csharp">
/// var builder = DistributedApplication.CreateBuilder(args);
/// 
/// var cache = builder.AddRedis("cache")
///                   .WithRedisInsight();
/// 
/// var api = builder.AddProject&lt;Projects.ApiService&gt;("api")
///                 .WithReference(cache);
/// 
/// builder.Build().Run();
/// </code>
/// </example>
```

### Container and Executable Resources

Document security considerations:

```csharp
/// <remarks>
/// <para>
/// As a security feature, Aspire doesn't run executables unless the command is located in a path 
/// listed in the PATH environment variable, or the full path is specified.
/// </para>
/// </remarks>
```

## Common Patterns and Phrases

### Method Summaries
- "Adds a [resource type] resource to the distributed application..."
- "Configures the [resource] to [specific behavior]..."
- "Creates a [type] with the specified [configuration]..."

### Remarks Sections
- "This method is typically used when..."
- "The [resource/parameter] is used to..."
- "Note that [important consideration]..."

### Examples
- Always show complete, realistic scenarios
- Include variable declarations and builder.Build().Run()
- Use meaningful resource names (not "foo", "bar")
- Show resource references and dependencies when relevant

## Code Example Standards

1. **Complete examples**: Show from builder creation to app.Run()
2. **Realistic names**: Use domain-appropriate names like "cache", "database", "api"
3. **Show relationships**: Demonstrate resource dependencies with WithReference()
4. **Include context**: Brief comments explaining what the example demonstrates
5. **Proper formatting**: Use proper indentation and spacing

## Exception Documentation

Document all exceptions that can be thrown:

```csharp
/// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> or <paramref name="name"/> is null.</exception>
/// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
/// <exception cref="InvalidOperationException">Thrown when a resource with the same name already exists.</exception>
```

## Cross-References

Use `<see cref=""/>` for:
- Related types and interfaces
- Related methods and properties
- External documentation links where appropriate

## Review Checklist

Before committing XML documentation:

- [ ] Summary is clear and concise (one sentence preferred)
- [ ] All parameters are documented with meaningful descriptions
- [ ] Return value is documented
- [ ] Exceptions are documented
- [ ] At least one practical code example is included
- [ ] Security implications are noted for sensitive parameters
- [ ] Cross-references use proper `<see cref=""/>` syntax
- [ ] Examples compile and show realistic usage
- [ ] Remarks section provides additional context when needed