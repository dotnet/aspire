// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREEXTENSION001
using Aspire.Hosting.ApplicationModel;
using Microsoft.Extensions.DependencyInjection;

namespace Aspire.Hosting;

/// <summary>
/// Provides extension methods for configuring stdin support.
/// </summary>
public static class StdinResourceBuilderExtensions
{
    /// <summary>
    /// Enables stdin support for a resource that supports receiving stdin input.
    /// </summary>
    /// <param name="builder">Builder for the resource.</param>
    /// <param name="enabled">Whether stdin support is enabled. Defaults to <c>true</c>.</param>
    /// <returns>The <see cref="IResourceBuilder{T}"/>.</returns>
    /// <remarks>
    /// When enabled, a "Send Input" command is added to the resource that allows users to send
    /// text input to the resource's stdin stream from the dashboard.
    /// </remarks>
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    public static IResourceBuilder<IResourceWithStdin> WithStdin(this IResourceBuilder<IResourceWithStdin> builder, bool enabled = true)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.WithAnnotation(new ResourceStdinAnnotation { Enabled = enabled }, ResourceAnnotationMutationBehavior.Replace);

        // Containers require an explicit stdin-open configuration (e.g., Docker -i).
        if (builder.Resource is ContainerResource)
        {
            builder.WithAnnotation(new ContainerStdinAnnotation { Enabled = enabled }, ResourceAnnotationMutationBehavior.Replace);
        }

        if (!enabled)
        {
            return builder;
        }

        var (title, description) = builder.Resource switch
        {
            ProjectResource => ("Send Input to Project", "Send text input to the project's stdin stream."),
            ExecutableResource => ("Send Input to Executable", "Send text input to the executable's stdin stream."),
            ContainerResource => ("Send Input to Container", "Send text input to the container's stdin stream."),
            _ => ("Send Input", "Send text input to the resource's stdin stream.")
        };

        builder.WithCommand(
            name: "send-stdin-input",
            displayName: "Send Input",
            executeCommand: async context =>
            {
                var interactionService = context.ServiceProvider.GetService<IInteractionService>();
                if (interactionService is null || !interactionService.IsAvailable)
                {
                    return CommandResults.Failure("Interaction service is not available.");
                }

                var inputResult = await interactionService.PromptInputAsync(
                    title: title,
                    message: $"Enter text to send to the stdin of '{context.ResourceName}':",
                    inputLabel: "Input",
                    placeHolder: "Type your input here...",
                    cancellationToken: context.CancellationToken).ConfigureAwait(false);

                if (inputResult.Canceled)
                {
                    return CommandResults.Canceled();
                }

                var consoleInputService = context.ServiceProvider.GetService<Dashboard.IResourceConsoleInputService>();
                if (consoleInputService is null)
                {
                    return CommandResults.Failure("Console input service is not available.");
                }

                try
                {
                    await consoleInputService.SendInputAsync(context.ResourceName, inputResult.Data!.Value + "\n", context.CancellationToken).ConfigureAwait(false);
                    return CommandResults.Success();
                }
                catch (InvalidOperationException ex)
                {
                    return CommandResults.Failure(ex.Message);
                }
            },
            commandOptions: new CommandOptions
            {
                UpdateState = context => context.ResourceSnapshot.State?.Text == KnownResourceStates.Running
                    ? ResourceCommandState.Enabled
                    : ResourceCommandState.Disabled,
                Description = description,
                IconName = "TextEditStyle"
            });

        return builder;
    }
#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
}
