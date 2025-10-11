// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Eventing;
using Aspire.Hosting.Lifecycle;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting.Tests;

public class RequiredCommandAnnotationTests
{
    [Fact]
    public void RequiredCommandAnnotation_StoresCommand()
    {
        // Arrange & Act
        var annotation = new RequiredCommandAnnotation("test-command");

        // Assert
        Assert.Equal("test-command", annotation.Command);
    }

    [Fact]
    public void RequiredCommandAnnotation_ThrowsOnNullCommand()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new RequiredCommandAnnotation(null!));
    }

    [Fact]
    public void RequiredCommandAnnotation_CanSetHelpLink()
    {
        // Arrange & Act
        var annotation = new RequiredCommandAnnotation("test-command")
        {
            HelpLink = "https://example.com/help"
        };

        // Assert
        Assert.Equal("https://example.com/help", annotation.HelpLink);
    }

    [Fact]
    public void RequiredCommandAnnotation_CanSetValidationCallback()
    {
        // Arrange
        Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>> callback = 
            (path, ct) => Task.FromResult((true, (string?)null));

        // Act
        var annotation = new RequiredCommandAnnotation("test-command")
        {
            ValidationCallback = callback
        };

        // Assert
        Assert.NotNull(annotation.ValidationCallback);
        Assert.Same(callback, annotation.ValidationCallback);
    }

    [Fact]
    public void WithRequiredCommand_AddsAnnotation()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("test", "image");

        // Act
        resourceBuilder.WithRequiredCommand("test-command");

        // Assert
        var annotation = resourceBuilder.Resource.Annotations.OfType<RequiredCommandAnnotation>().Single();
        Assert.Equal("test-command", annotation.Command);
        Assert.Null(annotation.HelpLink);
        Assert.Null(annotation.ValidationCallback);
    }

    [Fact]
    public void WithRequiredCommand_AddsAnnotationWithHelpLink()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("test", "image");

        // Act
        resourceBuilder.WithRequiredCommand("test-command", "https://example.com/help");

        // Assert
        var annotation = resourceBuilder.Resource.Annotations.OfType<RequiredCommandAnnotation>().Single();
        Assert.Equal("test-command", annotation.Command);
        Assert.Equal("https://example.com/help", annotation.HelpLink);
        Assert.Null(annotation.ValidationCallback);
    }

    [Fact]
    public void WithRequiredCommand_AddsAnnotationWithValidationCallback()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("test", "image");
        Func<string, CancellationToken, Task<(bool IsValid, string? ValidationMessage)>> callback = 
            (path, ct) => Task.FromResult((true, (string?)null));

        // Act
        resourceBuilder.WithRequiredCommand("test-command", callback);

        // Assert
        var annotation = resourceBuilder.Resource.Annotations.OfType<RequiredCommandAnnotation>().Single();
        Assert.Equal("test-command", annotation.Command);
        Assert.Null(annotation.HelpLink);
        Assert.NotNull(annotation.ValidationCallback);
    }

    [Fact]
    public void WithRequiredCommand_CanAddMultipleAnnotations()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("test", "image");

        // Act
        resourceBuilder
            .WithRequiredCommand("command1")
            .WithRequiredCommand("command2");

        // Assert
        var annotations = resourceBuilder.Resource.Annotations.OfType<RequiredCommandAnnotation>().ToList();
        Assert.Equal(2, annotations.Count);
        Assert.Equal("command1", annotations[0].Command);
        Assert.Equal("command2", annotations[1].Command);
    }

    [Fact]
    public void WithRequiredCommand_ThrowsOnNullBuilder()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            RequiredCommandResourceExtensions.WithRequiredCommand<ContainerResource>(null!, "test"));
    }

    [Fact]
    public void WithRequiredCommand_ThrowsOnNullCommand()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var resourceBuilder = builder.AddContainer("test", "image");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => resourceBuilder.WithRequiredCommand(null!));
    }

    [Fact]
    public async Task RequiredCommandValidationLifecycleHook_IsRegistered()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();

        // Act
        await using var app = builder.Build();

        // Assert - The hook should be registered as an eventing subscriber
        var subscribers = app.Services.GetServices<IDistributedApplicationEventingSubscriber>();
        Assert.Contains(subscribers, s => s is RequiredCommandValidationLifecycleHook);
    }

    [Fact]
    public async Task RequiredCommandValidationLifecycleHook_ValidatesExistingCommand()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        
        // Use a command that should exist on all platforms
        var command = OperatingSystem.IsWindows() ? "cmd" : "sh";
        builder.AddContainer("test", "image").WithRequiredCommand(command);

        await using var app = builder.Build();

        // Act - Start the application which triggers BeforeResourceStartedEvent
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = appModel.Resources.Single(r => r.Name == "test");
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();

        // Assert - Should not throw when command exists
        await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, app.Services));
    }

    [Fact]
    public async Task RequiredCommandValidationLifecycleHook_ThrowsForMissingCommand()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("test", "image").WithRequiredCommand("this-command-definitely-does-not-exist-12345");

        await using var app = builder.Build();

        // Act
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = appModel.Resources.Single(r => r.Name == "test");
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();

        // Assert - Should throw when command doesn't exist
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            async () => await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, app.Services)));
        
        Assert.Contains("this-command-definitely-does-not-exist-12345", exception.Message);
    }

    [Fact]
    public async Task RequiredCommandValidationLifecycleHook_IncludesHelpLinkInError()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        builder.AddContainer("test", "image")
            .WithRequiredCommand("missing-command", "https://example.com/install");

        await using var app = builder.Build();

        // Act
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = appModel.Resources.Single(r => r.Name == "test");
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();

        // Assert
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            async () => await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, app.Services)));
        
        Assert.Contains("missing-command", exception.Message);
        Assert.Contains("https://example.com/install", exception.Message);
    }

    [Fact]
    public async Task RequiredCommandValidationLifecycleHook_CallsValidationCallback()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var callbackInvoked = false;
        var command = OperatingSystem.IsWindows() ? "cmd" : "sh";

        builder.AddContainer("test", "image")
            .WithRequiredCommand(command, (path, ct) =>
            {
                callbackInvoked = true;
                return Task.FromResult((true, (string?)null));
            });

        await using var app = builder.Build();

        // Act
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = appModel.Resources.Single(r => r.Name == "test");
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();
        await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, app.Services));

        // Assert
        Assert.True(callbackInvoked);
    }

    [Fact]
    public async Task RequiredCommandValidationLifecycleHook_ThrowsOnFailedValidationCallback()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var command = OperatingSystem.IsWindows() ? "cmd" : "sh";

        builder.AddContainer("test", "image")
            .WithRequiredCommand(command, (path, ct) =>
            {
                return Task.FromResult<(bool IsValid, string? ValidationMessage)>((false, "Custom validation failed"));
            });

        await using var app = builder.Build();

        // Act
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = appModel.Resources.Single(r => r.Name == "test");
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();

        // Assert
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            async () => await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, app.Services)));
        
        Assert.Contains("Custom validation failed", exception.Message);
    }

    [Fact]
    public async Task RequiredCommandValidationLifecycleHook_ValidatesMultipleAnnotations()
    {
        // Arrange
        var builder = DistributedApplication.CreateBuilder();
        var command = OperatingSystem.IsWindows() ? "cmd" : "sh";

        builder.AddContainer("test", "image")
            .WithRequiredCommand(command)
            .WithRequiredCommand("missing-command-xyz");

        await using var app = builder.Build();

        // Act
        var appModel = app.Services.GetRequiredService<DistributedApplicationModel>();
        var resource = appModel.Resources.Single(r => r.Name == "test");
        var eventing = app.Services.GetRequiredService<IDistributedApplicationEventing>();

        // Assert - Should throw on first missing command
        var exception = await Assert.ThrowsAsync<DistributedApplicationException>(
            async () => await eventing.PublishAsync(new BeforeResourceStartedEvent(resource, app.Services)));
        
        Assert.Contains("missing-command-xyz", exception.Message);
    }
}
