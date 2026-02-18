// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREEXTENSION001 // Experimental extension APIs
#pragma warning disable IDE0005 // Remove unnecessary using

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Aspire.Hosting.Tests;

public class DebuggerPropertiesAnnotationTests
{
    #region Test IDE Debugger Properties

    // Simulates VS Code debugger properties - inherits from VSCodeDebuggerPropertiesBase
    // which has VS Code-specific properties (Presentation, PreLaunchTask, etc.) directly on it
    [Experimental("ASPIREEXTENSION001")]
    private sealed class TestVSCodeDebuggerProperties : VSCodeDebuggerPropertiesBase
    {
        public override string Type { get; set; } = "coreclr";
        public override required string Name { get; set; }
        public override required string WorkingDirectory { get; init; }
        public bool WasConfigured { get; set; }
        public string? VSCodeSpecificValue { get; set; }
    }

    // Simulates another IDE's debugger properties - inherits from DebugAdapterProperties (no VS Code options)
    [Experimental("ASPIREEXTENSION001")]
    private sealed class TestOtherIdeDebuggerProperties : DebugAdapterProperties
    {
        public override string Type { get; set; } = "dotnet";
        public override required string Name { get; set; }
        public override required string WorkingDirectory { get; init; }
        public bool WasConfigured { get; set; }
        public string? OtherIdeSpecificValue { get; set; }
    }

    // Simulates Visual Studio debugger properties - inherits from DebugAdapterProperties (no VS Code options)
    [Experimental("ASPIREEXTENSION001")]
    private sealed class TestVisualStudioDebuggerProperties : DebugAdapterProperties
    {
        public override string Type { get; set; } = "managed";
        public override required string Name { get; set; }
        public override required string WorkingDirectory { get; init; }
        public bool WasConfigured { get; set; }
        public string? VisualStudioSpecificValue { get; set; }
    }

    #endregion

    [Fact]
    public void ConfigureDebuggerProperties_OnlyConfiguresMatchingType()
    {
        // Arrange - Create annotation for VS Code
        var annotation = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.WasConfigured = true;
            props.VSCodeSpecificValue = "configured";
        });

        // Create properties for different IDEs
        var vsCodeProps = new TestVSCodeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };
        var otherIdeProps = new TestOtherIdeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };
        var vsProps = new TestVisualStudioDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };

        // Act - Try to configure each type
        annotation.ConfigureDebuggerProperties(vsCodeProps);
        annotation.ConfigureDebuggerProperties(otherIdeProps);
        annotation.ConfigureDebuggerProperties(vsProps);

        // Assert - Only VS Code props should be configured
        Assert.True(vsCodeProps.WasConfigured);
        Assert.Equal("configured", vsCodeProps.VSCodeSpecificValue);

        Assert.False(otherIdeProps.WasConfigured);
        Assert.Null(otherIdeProps.OtherIdeSpecificValue);

        Assert.False(vsProps.WasConfigured);
        Assert.Null(vsProps.VisualStudioSpecificValue);
    }

    [Fact]
    public void MultipleAnnotations_EachConfiguresOnlyMatchingType()
    {
        // Arrange - Create annotations for each IDE
        var vsCodeAnnotation = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.WasConfigured = true;
            props.VSCodeSpecificValue = "vscode-value";
        });

        var otherIdeAnnotation = new ExecutableDebuggerPropertiesAnnotation<TestOtherIdeDebuggerProperties>(props =>
        {
            props.WasConfigured = true;
            props.OtherIdeSpecificValue = "other-ide-value";
        });

        var vsAnnotation = new ExecutableDebuggerPropertiesAnnotation<TestVisualStudioDebuggerProperties>(props =>
        {   
            props.WasConfigured = true;
            props.VisualStudioSpecificValue = "vs-value";
        });

        var annotations = new IDebuggerPropertiesAnnotation[] { vsCodeAnnotation, otherIdeAnnotation, vsAnnotation };

        // Scenario 1: VS Code IDE creates VS Code properties
        var vsCodeProps = new TestVSCodeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };
        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(vsCodeProps);
        }

        Assert.True(vsCodeProps.WasConfigured);
        Assert.Equal("vscode-value", vsCodeProps.VSCodeSpecificValue);

        // Scenario 2: Other IDE creates its properties
        var otherIdeProps = new TestOtherIdeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };
        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(otherIdeProps);
        }

        Assert.True(otherIdeProps.WasConfigured);
        Assert.Equal("other-ide-value", otherIdeProps.OtherIdeSpecificValue);

        // Scenario 3: Visual Studio IDE creates VS properties
        var vsProps = new TestVisualStudioDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };
        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(vsProps);
        }

        Assert.True(vsProps.WasConfigured);
        Assert.Equal("vs-value", vsProps.VisualStudioSpecificValue);
    }

    [Fact]
    public void Annotation_DoesNotThrow_WhenNonMatchingTypeProvided()
    {
        // Arrange
        var annotation = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.WasConfigured = true;
        });

        var otherIdeProps = new TestOtherIdeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };

        // Act & Assert - Should not throw, just silently skip
        var exception = Record.Exception(() => annotation.ConfigureDebuggerProperties(otherIdeProps));

        Assert.Null(exception);
        Assert.False(otherIdeProps.WasConfigured);
    }

    [Fact]
    public void ConfigureDebuggerPropertiesTyped_DirectlyConfiguresMatchingType()
    {
        // Arrange
        var annotation = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.WasConfigured = true;
            props.VSCodeSpecificValue = "direct";
        });

        var vsCodeProps = new TestVSCodeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };

        // Act - Use the typed method directly
        annotation.ConfigureDebuggerPropertiesTyped(vsCodeProps);

        // Assert
        Assert.True(vsCodeProps.WasConfigured);
        Assert.Equal("direct", vsCodeProps.VSCodeSpecificValue);
    }

    [Fact]
    public void IDebuggerPropertiesAnnotation_CanBeUsedPolymorphically()
    {
        // Arrange
        IDebuggerPropertiesAnnotation annotation = new ExecutableDebuggerPropertiesAnnotation<TestOtherIdeDebuggerProperties>(props =>
        {
            props.WasConfigured = true;
            props.OtherIdeSpecificValue = "polymorphic";
        });

        var otherIdeProps = new TestOtherIdeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };
        var vsCodeProps = new TestVSCodeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };

        // Act - Use through the interface
        annotation.ConfigureDebuggerProperties(otherIdeProps);
        annotation.ConfigureDebuggerProperties(vsCodeProps);

        // Assert
        Assert.True(otherIdeProps.WasConfigured);
        Assert.Equal("polymorphic", otherIdeProps.OtherIdeSpecificValue);

        Assert.False(vsCodeProps.WasConfigured);
    }

    [Fact]
    public void SimulateIdeWorkflow_MultipleIdeSupportOnSameResource()
    {
        // This test simulates how an AppHost developer would configure a resource
        // to support multiple IDEs, and how each IDE would consume it

        // Arrange - AppHost developer adds multiple IDE configurations
        var annotations = new List<IDebuggerPropertiesAnnotation>
        {
            new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
            {
                props.VSCodeSpecificValue = "stopOnEntry=true";
            }),
            new ExecutableDebuggerPropertiesAnnotation<TestOtherIdeDebuggerProperties>(props =>
            {
                props.OtherIdeSpecificValue = "enableExternalSource=true";
            }),
            new ExecutableDebuggerPropertiesAnnotation<TestVisualStudioDebuggerProperties>(props =>
            {
                props.VisualStudioSpecificValue = "enableNativeDebugging=false";
            })
        };

        // Act - Simulate VS Code IDE extension consuming the resource
        var vsCodeDebugConfig = new TestVSCodeDebuggerProperties
        {
            Name = "MyProject",
            WorkingDirectory = "/path/to/project"
        };

        // The IDE iterates over all annotations and calls ConfigureDebuggerProperties
        // Only matching annotations will have any effect
        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(vsCodeDebugConfig);
        }

        // Assert - Only VS Code-specific configuration was applied
        Assert.Equal("stopOnEntry=true", vsCodeDebugConfig.VSCodeSpecificValue);

        // Act - Simulate another IDE extension consuming the same resource
        var otherIdeDebugConfig = new TestOtherIdeDebuggerProperties
        {
            Name = "MyProject",
            WorkingDirectory = "/path/to/project"
        };

        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(otherIdeDebugConfig);
        }

        // Assert - Only the other IDE's configuration was applied
        Assert.Equal("enableExternalSource=true", otherIdeDebugConfig.OtherIdeSpecificValue);
    }

    [Fact]
    public void Annotation_ModifiesExistingProperties_NotReplaceThem()
    {
        // Arrange - Annotation modifies specific properties
        var annotation = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.VSCodeSpecificValue = "modified";
            // Does NOT modify Name or WorkingDirectory
        });

        var props = new TestVSCodeDebuggerProperties
        {
            Name = "OriginalName",
            WorkingDirectory = "/original/path",
            Type = "original-type"
        };

        // Act
        annotation.ConfigureDebuggerProperties(props);

        // Assert - Only VSCodeSpecificValue was modified, others remain
        Assert.Equal("OriginalName", props.Name);
        Assert.Equal("/original/path", props.WorkingDirectory);
        Assert.Equal("original-type", props.Type);
        Assert.Equal("modified", props.VSCodeSpecificValue);
    }

    [Fact]
    public void AnnotationsAreCumulative_MultipleAnnotationsOfSameType()
    {
        // Arrange - Multiple annotations for the same IDE type
        var annotation1 = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.VSCodeSpecificValue = "first";
        });

        var annotation2 = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.Type = "modified-type";  // Different property
        });

        var annotation3 = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.VSCodeSpecificValue = "third";  // Overrides first
        });

        var annotations = new IDebuggerPropertiesAnnotation[] { annotation1, annotation2, annotation3 };

        var props = new TestVSCodeDebuggerProperties
        {
            Name = "Test",
            WorkingDirectory = "/test",
            Type = "original"
        };

        // Act - Apply all annotations in order
        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(props);
        }

        // Assert - All annotations were applied, later ones override earlier ones
        Assert.Equal("modified-type", props.Type);
        Assert.Equal("third", props.VSCodeSpecificValue);  // Last one wins
    }

    [Fact]
    public void VSCodeDebuggerPropertiesBase_SerializesVSCodeOptionsAtTopLevel()
    {
        // This test verifies that VS Code-specific options (presentation, preLaunchTask, etc.)
        // serialize directly on debugger_properties, NOT nested under a 'vscode' key.
        // This is critical for the VS Code extension which spreads debugger_properties directly.

        // Arrange
        var props = new TestVSCodeDebuggerProperties
        {
            Name = "TestProject",
            WorkingDirectory = "/test/path",
            Type = "coreclr",
            Request = "launch",
            Presentation = new PresentationOptions
            {
                Order = 1,
                Group = "Aspire",
                Hidden = false
            },
            PreLaunchTask = "${defaultBuildTask}",
            PostDebugTask = "cleanup",
            ServerReadyAction = new ServerReadyAction
            {
                Action = "openExternally",
                Pattern = @"Now listening on: (https?://\S+)"
            }
        };

        // Act - Serialize to JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(props, options);

        // Assert - VS Code options should be at top level, NOT nested under 'vscode'
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Core DAP properties
        Assert.Equal("coreclr", root.GetProperty("type").GetString());
        Assert.Equal("TestProject", root.GetProperty("name").GetString());
        Assert.Equal("launch", root.GetProperty("request").GetString());
        Assert.Equal("/test/path", root.GetProperty("cwd").GetString());

        // VS Code options should be at top level
        Assert.True(root.TryGetProperty("presentation", out var presentation));
        Assert.Equal(1, presentation.GetProperty("order").GetInt32());
        Assert.Equal("Aspire", presentation.GetProperty("group").GetString());

        Assert.True(root.TryGetProperty("preLaunchTask", out var preLaunchTask));
        Assert.Equal("${defaultBuildTask}", preLaunchTask.GetString());

        Assert.True(root.TryGetProperty("postDebugTask", out var postDebugTask));
        Assert.Equal("cleanup", postDebugTask.GetString());

        Assert.True(root.TryGetProperty("serverReadyAction", out var serverReadyAction));
        Assert.Equal("openExternally", serverReadyAction.GetProperty("action").GetString());

        // There should NOT be a 'vscode' property
        Assert.False(root.TryGetProperty("vscode", out _));
    }

    [Fact]
    public void VSCodeDebuggerPropertiesBase_CanConfigureVSCodeOptionsViaAnnotation()
    {
        // Arrange
        var annotation = new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
        {
            props.Presentation = new PresentationOptions { Order = 5, Group = "Testing" };
            props.PreLaunchTask = "build";
        });

        var props = new TestVSCodeDebuggerProperties
        {
            Name = "Test",
            WorkingDirectory = "/test"
        };

        // Act
        annotation.ConfigureDebuggerProperties(props);

        // Assert - VS Code options should be set directly on the properties
        Assert.NotNull(props.Presentation);
        Assert.Equal(5, props.Presentation.Order);
        Assert.Equal("Testing", props.Presentation.Group);
        Assert.Equal("build", props.PreLaunchTask);
    }

    [Fact]
    public void NonVSCodeDebuggerProperties_DoNotHaveVSCodeOptions()
    {
        // This test verifies that non-VS Code debugger properties
        // don't have the VS Code-specific options

        // Arrange
        var otherIdeProps = new TestOtherIdeDebuggerProperties
        {
            Name = "Test",
            WorkingDirectory = "/test",
            OtherIdeSpecificValue = "other-ide-specific"
        };

        // Act - Serialize to JSON
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(otherIdeProps, options);

        // Assert
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Core DAP properties should exist
        Assert.True(root.TryGetProperty("type", out _));
        Assert.True(root.TryGetProperty("name", out _));
        Assert.True(root.TryGetProperty("cwd", out _));

        // VS Code-specific properties should NOT exist
        Assert.False(root.TryGetProperty("presentation", out _));
        Assert.False(root.TryGetProperty("preLaunchTask", out _));
        Assert.False(root.TryGetProperty("postDebugTask", out _));
        Assert.False(root.TryGetProperty("serverReadyAction", out _));
        Assert.False(root.TryGetProperty("vscode", out _));
    }

    #region Simulated Other IDE Support

    /// <summary>
    /// Simulates what another IDE would define as debugger properties for .NET projects.
    /// Extends DebugAdapterProperties directly (not VSCodeDebuggerPropertiesBase) since
    /// each IDE has its own IDE-specific options.
    /// </summary>
    [Experimental("ASPIREEXTENSION001")]
    private sealed class OtherIdeDotNetDebuggerProperties : DebugAdapterProperties
    {
        public override string Type { get; set; } = "dotnet";
        public override required string Name { get; set; }
        public override required string WorkingDirectory { get; init; }

        // Other IDE-specific options (hypothetical)
        [JsonPropertyName("externalSourcesSupport")]
        public bool? ExternalSourcesSupport { get; set; }

        [JsonPropertyName("breakOnUserUnhandledExceptions")]
        public bool? BreakOnUserUnhandledExceptions { get; set; }

        [JsonPropertyName("allowDynamicCode")]
        public bool? AllowDynamicCode { get; set; }

        [JsonPropertyName("enableHotReload")]
        public bool? EnableHotReload { get; set; }

        [JsonPropertyName("debuggerPort")]
        public int? DebuggerPort { get; set; }
    }

    /// <summary>
    /// Simulates a launch configuration that could hold any debugger properties.
    /// In the real implementation, this would be something like ProjectLaunchConfiguration
    /// but generic enough to hold any debugger properties type.
    /// </summary>
    [Experimental("ASPIREEXTENSION001")]
    private sealed class TestLaunchConfiguration<T> where T : DebugAdapterProperties
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "project";

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "Debug";

        [JsonPropertyName("debugger_properties")]
        public T? DebuggerProperties { get; set; }

        [JsonPropertyName("project_path")]
        public string ProjectPath { get; set; } = "";
    }

    #endregion

    [Fact]
    public void SimulateOtherIdeSupport_FullEndToEndScenario()
    {
        // This test simulates the full scenario of how another IDE would:
        // 1. Define their own debugger properties class
        // 2. Register configuration via annotations
        // 3. Create launch configurations with their debugger properties
        // 4. Serialize everything correctly for their IDE to consume

        // ========================================
        // STEP 1: AppHost developer configures resources for multiple IDEs
        // ========================================

        // Annotations that would be added to a resource via WithVSCodeDebugging and a hypothetical WithOtherIdeDebugging
        var annotations = new List<IDebuggerPropertiesAnnotation>
        {
            // VS Code configuration
            new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
            {
                props.Presentation = new PresentationOptions { Order = 1, Group = "Aspire" };
                props.PreLaunchTask = "${defaultBuildTask}";
            }),

            // Other IDE configuration (hypothetical extension method: WithOtherIdeDebugging)
            new ExecutableDebuggerPropertiesAnnotation<OtherIdeDotNetDebuggerProperties>(props =>
            {
                props.ExternalSourcesSupport = true;
                props.BreakOnUserUnhandledExceptions = true;
                props.EnableHotReload = true;
            })
        };

        // ========================================
        // STEP 2: Other IDE creates its debugger properties and applies annotations
        // ========================================

        var otherIdeDebuggerProps = new OtherIdeDotNetDebuggerProperties
        {
            Name = "MyApi",
            WorkingDirectory = "/path/to/project",
            Type = "dotnet",
            Request = "launch"
        };

        // Apply all annotations - only the matching one will have effect
        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(otherIdeDebuggerProps);
        }

        // ========================================
        // STEP 3: Create the launch configuration with the IDE's properties
        // ========================================

        var launchConfig = new TestLaunchConfiguration<OtherIdeDotNetDebuggerProperties>
        {
            Type = "project",
            Mode = "Debug",
            ProjectPath = "/path/to/project/MyApi.csproj",
            DebuggerProperties = otherIdeDebuggerProps
        };

        // ========================================
        // STEP 4: Serialize and verify the JSON structure
        // ========================================

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(launchConfig, options);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify top-level launch configuration properties
        Assert.Equal("project", root.GetProperty("type").GetString());
        Assert.Equal("Debug", root.GetProperty("mode").GetString());
        Assert.Equal("/path/to/project/MyApi.csproj", root.GetProperty("project_path").GetString());

        // Verify debugger_properties exists and contains the right structure
        Assert.True(root.TryGetProperty("debugger_properties", out var debuggerProps));

        // Core DAP properties
        Assert.Equal("dotnet", debuggerProps.GetProperty("type").GetString());
        Assert.Equal("MyApi", debuggerProps.GetProperty("name").GetString());
        Assert.Equal("launch", debuggerProps.GetProperty("request").GetString());
        Assert.Equal("/path/to/project", debuggerProps.GetProperty("cwd").GetString());

        // IDE-specific properties (configured via annotation)
        Assert.True(debuggerProps.GetProperty("externalSourcesSupport").GetBoolean());
        Assert.True(debuggerProps.GetProperty("breakOnUserUnhandledExceptions").GetBoolean());
        Assert.True(debuggerProps.GetProperty("enableHotReload").GetBoolean());

        // VS Code-specific properties should NOT be present (other IDE doesn't inherit from VSCodeDebuggerPropertiesBase)
        Assert.False(debuggerProps.TryGetProperty("presentation", out _));
        Assert.False(debuggerProps.TryGetProperty("preLaunchTask", out _));
        Assert.False(debuggerProps.TryGetProperty("serverReadyAction", out _));
        Assert.False(debuggerProps.TryGetProperty("vscode", out _));
    }

    [Fact]
    public void SimulateVSCodeIdeSupport_FullEndToEndScenario()
    {
        // This test shows the VS Code side of the same scenario for comparison

        // ========================================
        // STEP 1: Same annotations as other IDE test
        // ========================================

        var annotations = new List<IDebuggerPropertiesAnnotation>
        {
            new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
            {
                props.Presentation = new PresentationOptions { Order = 1, Group = "Aspire" };
                props.PreLaunchTask = "${defaultBuildTask}";
                props.ServerReadyAction = new ServerReadyAction
                {
                    Action = "openExternally",
                    Pattern = @"Now listening on: (https?://\S+)"
                };
            }),

            new ExecutableDebuggerPropertiesAnnotation<OtherIdeDotNetDebuggerProperties>(props =>
            {
                props.ExternalSourcesSupport = true;
                props.EnableHotReload = true;
            })
        };

        // ========================================
        // STEP 2: VS Code IDE creates its debugger properties and applies annotations
        // ========================================

        var vsCodeDebuggerProps = new TestVSCodeDebuggerProperties
        {
            Name = "MyApi",
            WorkingDirectory = "/path/to/project",
            Type = "coreclr",
            Request = "launch"
        };

        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(vsCodeDebuggerProps);
        }

        // ========================================
        // STEP 3: Create the launch configuration with VS Code properties
        // ========================================

        var launchConfig = new TestLaunchConfiguration<TestVSCodeDebuggerProperties>
        {
            Type = "project",
            Mode = "Debug",
            ProjectPath = "/path/to/project/MyApi.csproj",
            DebuggerProperties = vsCodeDebuggerProps
        };

        // ========================================
        // STEP 4: Serialize and verify the JSON structure
        // ========================================

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(launchConfig, options);

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        // Verify debugger_properties exists
        Assert.True(root.TryGetProperty("debugger_properties", out var debuggerProps));

        // Core DAP properties
        Assert.Equal("coreclr", debuggerProps.GetProperty("type").GetString());
        Assert.Equal("MyApi", debuggerProps.GetProperty("name").GetString());

        // VS Code-specific properties ARE present at top level of debugger_properties
        Assert.True(debuggerProps.TryGetProperty("presentation", out var presentation));
        Assert.Equal(1, presentation.GetProperty("order").GetInt32());
        Assert.Equal("Aspire", presentation.GetProperty("group").GetString());

        Assert.Equal("${defaultBuildTask}", debuggerProps.GetProperty("preLaunchTask").GetString());

        Assert.True(debuggerProps.TryGetProperty("serverReadyAction", out var serverReadyAction));
        Assert.Equal("openExternally", serverReadyAction.GetProperty("action").GetString());

        // Other IDE-specific properties should NOT be present
        Assert.False(debuggerProps.TryGetProperty("externalSourcesSupport", out _));
        Assert.False(debuggerProps.TryGetProperty("enableHotReload", out _));

        // No nested 'vscode' property
        Assert.False(debuggerProps.TryGetProperty("vscode", out _));
    }

    [Fact]
    public void BothIdesCanCoexist_SameResourceDifferentConfigurations()
    {
        // This test verifies that both IDEs can be configured on the same resource
        // and each gets their own distinct configuration

        var annotations = new List<IDebuggerPropertiesAnnotation>
        {
            new ExecutableDebuggerPropertiesAnnotation<TestVSCodeDebuggerProperties>(props =>
            {
                props.PreLaunchTask = "vscode-build";
                props.Presentation = new PresentationOptions { Hidden = false };
            }),
            new ExecutableDebuggerPropertiesAnnotation<OtherIdeDotNetDebuggerProperties>(props =>
            {
                props.EnableHotReload = true;
                props.DebuggerPort = 5005;
            })
        };

        // Create both configurations
        var vsCodeProps = new TestVSCodeDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };
        var otherIdeProps = new OtherIdeDotNetDebuggerProperties { Name = "Test", WorkingDirectory = "/test" };

        // Apply all annotations to both
        foreach (var annotation in annotations)
        {
            annotation.ConfigureDebuggerProperties(vsCodeProps);
            annotation.ConfigureDebuggerProperties(otherIdeProps);
        }

        // Verify VS Code got VS Code config only
        Assert.Equal("vscode-build", vsCodeProps.PreLaunchTask);
        Assert.NotNull(vsCodeProps.Presentation);
        Assert.False(vsCodeProps.Presentation.Hidden);

        // Verify other IDE got its config only
        Assert.True(otherIdeProps.EnableHotReload);
        Assert.Equal(5005, otherIdeProps.DebuggerPort);

        // Serialize both and verify no cross-contamination
        var options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

        var vsCodeJson = JsonSerializer.Serialize(vsCodeProps, options);
        var otherIdeJson = JsonSerializer.Serialize(otherIdeProps, options);

        using var vsCodeDoc = JsonDocument.Parse(vsCodeJson);
        using var otherIdeDoc = JsonDocument.Parse(otherIdeJson);

        // VS Code JSON should have VS Code props, not other IDE props
        Assert.True(vsCodeDoc.RootElement.TryGetProperty("preLaunchTask", out _));
        Assert.False(vsCodeDoc.RootElement.TryGetProperty("enableHotReload", out _));
        Assert.False(vsCodeDoc.RootElement.TryGetProperty("debuggerPort", out _));

        // Other IDE JSON should have its props, not VS Code props
        Assert.True(otherIdeDoc.RootElement.TryGetProperty("enableHotReload", out _));
        Assert.True(otherIdeDoc.RootElement.TryGetProperty("debuggerPort", out _));
        Assert.False(otherIdeDoc.RootElement.TryGetProperty("preLaunchTask", out _));
        Assert.False(otherIdeDoc.RootElement.TryGetProperty("presentation", out _));
    }

    [Fact]
    public void PolymorphicSerialization_ThroughBaseTypeReference_PreservesDerivedTypeProperties()
    {
        // This test verifies that when a derived debugger properties type is serialized
        // through a base type reference (as happens in the real launch configuration classes),
        // all properties from the derived type are preserved.
        //
        // This is the real scenario: NodeLaunchConfiguration uses DebugAdapterProperties as
        // its generic type parameter, but we assign VSCodeNodeDebuggerProperties to it.

        // Arrange - Create launch configuration similar to real NodeLaunchConfiguration
        // which uses ExecutableLaunchConfigurationWithDebuggerProperties<DebugAdapterProperties>
        var launchConfig = new TestLaunchConfigurationWithBaseType
        {
            Type = "node",
            Mode = "Debug",
            // DebuggerProperties is typed as DebugAdapterProperties (base type)
            // but we assign a VSCodeNodeDebuggerProperties instance
            DebuggerProperties = new TestVSCodeDebuggerProperties
            {
                Name = "Debug Node App",
                WorkingDirectory = "/path/to/app",
                Type = "node",
                Request = "launch",
                // VS Code-specific properties
                PreLaunchTask = "${defaultBuildTask}",
                Presentation = new PresentationOptions { Order = 1, Group = "Aspire" },
                ServerReadyAction = new ServerReadyAction { Action = "openExternally" }
            }
        };

        // Act - Serialize the launch configuration
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        var json = JsonSerializer.Serialize(launchConfig, options);

        // Assert - Verify derived type properties are included in the JSON
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.True(root.TryGetProperty("debugger_properties", out var debuggerProps));

        // Base DAP properties should be present
        Assert.Equal("node", debuggerProps.GetProperty("type").GetString());
        Assert.Equal("Debug Node App", debuggerProps.GetProperty("name").GetString());
        Assert.Equal("launch", debuggerProps.GetProperty("request").GetString());

        // VS Code-specific properties should also be present (this is the key assertion)
        Assert.True(debuggerProps.TryGetProperty("preLaunchTask", out var preLaunchTask),
            "VS Code-specific property 'preLaunchTask' should be serialized through base type reference");
        Assert.Equal("${defaultBuildTask}", preLaunchTask.GetString());

        Assert.True(debuggerProps.TryGetProperty("presentation", out var presentation),
            "VS Code-specific property 'presentation' should be serialized through base type reference");
        Assert.Equal(1, presentation.GetProperty("order").GetInt32());

        Assert.True(debuggerProps.TryGetProperty("serverReadyAction", out var serverReady),
            "VS Code-specific property 'serverReadyAction' should be serialized through base type reference");
        Assert.Equal("openExternally", serverReady.GetProperty("action").GetString());
    }

    /// <summary>
    /// Simulates the real launch configuration pattern where DebuggerProperties
    /// is typed as the base type (DebugAdapterProperties) but can hold derived types.
    /// </summary>
    [Experimental("ASPIREEXTENSION001")]
    private sealed class TestLaunchConfigurationWithBaseType
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "project";

        [JsonPropertyName("mode")]
        public string Mode { get; set; } = "Debug";

        /// <summary>
        /// This is typed as the base type, mimicking how NodeLaunchConfiguration,
        /// ProjectLaunchConfiguration, etc. declare their DebuggerProperties property.
        /// </summary>
        [JsonPropertyName("debugger_properties")]
        public DebugAdapterProperties? DebuggerProperties { get; set; }
    }
}
