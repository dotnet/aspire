// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Aspire.Hosting.Publishing;

/// <summary>
/// Service for requesting user prompts during publishing operations.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public interface IPublishingPromptService
{
    /// <summary>
    /// Prompts the user for a string input.
    /// </summary>
    /// <param name="promptText">The text to display to the user.</param>
    /// <param name="defaultValue">The default value for the input.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's input string.</returns>
    Task<string?> PromptForStringAsync(string promptText, string? defaultValue = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts the user for a confirmation (yes/no).
    /// </summary>
    /// <param name="promptText">The text to display to the user.</param>
    /// <param name="defaultValue">The default value for the confirmation.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's confirmation response.</returns>
    Task<bool> PromptForConfirmationAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prompts the user to select from multiple choices.
    /// </summary>
    /// <param name="promptText">The text to display to the user.</param>
    /// <param name="choices">The available choices.</param>
    /// <param name="allowMultiple">Whether multiple selections are allowed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The user's selected choice(s).</returns>
    Task<string[]> PromptForSelectionAsync(string promptText, string[] choices, bool allowMultiple = false, CancellationToken cancellationToken = default);
}

/// <summary>
/// Default implementation of the publishing prompt service.
/// </summary>
[Experimental("ASPIREPUBLISHERS001", UrlFormat = "https://aka.ms/aspire/diagnostics/{0}")]
public sealed class PublishingPromptService(IPublishingActivityProgressReporter activityReporter) : IPublishingPromptService
{
    private static int s_promptCounter;

    /// <inheritdoc/>
    public async Task<string?> PromptForStringAsync(string promptText, string? defaultValue = null, CancellationToken cancellationToken = default)
    {
        var promptData = new PromptForStringData { PromptText = promptText, DefaultValue = defaultValue };

        return await SendPromptRequestAsync(
            promptText,
            PromptActivityType.PromptForString,
            promptData,
            response => response ?? defaultValue,
            result => $"Received input: {result ?? "(empty)"}",
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<bool> PromptForConfirmationAsync(string promptText, bool defaultValue = true, CancellationToken cancellationToken = default)
    {
        var promptData = new PromptForConfirmationData { PromptText = promptText, DefaultValue = defaultValue };

        return await SendPromptRequestAsync(
            promptText,
            PromptActivityType.PromptForConfirmation,
            promptData,
            response => response switch
            {
                "true" => true,
                "false" => false,
                _ => defaultValue
            },
            result => $"Confirmed: {(result ? "Yes" : "No")}",
            cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async Task<string[]> PromptForSelectionAsync(string promptText, string[] choices, bool allowMultiple = false, CancellationToken cancellationToken = default)
    {
        var promptData = new PromptForSelectionData { PromptText = promptText, Choices = choices, AllowMultiple = allowMultiple };

        return await SendPromptRequestAsync(
            promptText,
            PromptActivityType.PromptForSelection,
            promptData,
            response =>
            {
                // Parse the response (comma-separated for multiple selections)
                return string.IsNullOrEmpty(response)
                    ? Array.Empty<string>()
                    : response.Split(',', StringSplitOptions.RemoveEmptyEntries);
            },
            result => $"Selected: {string.Join(", ", result)}",
            cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Helper method that encapsulates the pattern for sending prompt requests, waiting for responses, and resolving values.
    /// </summary>
    /// <typeparam name="T">The type of the prompt data.</typeparam>
    /// <typeparam name="TResult">The type of the result after processing the response.</typeparam>
    /// <param name="promptText">The text to display to the user.</param>
    /// <param name="promptType">The type of prompt activity.</param>
    /// <param name="promptData">The prompt data to serialize.</param>
    /// <param name="responseProcessor">Function to process the raw response into the desired result type.</param>
    /// <param name="resultFormatter">Function to format the result for display in the completion message.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The processed result from the user's response.</returns>
    private async Task<TResult> SendPromptRequestAsync<T, TResult>(
        string promptText,
        PromptActivityType promptType,
        T promptData,
        Func<string?, TResult> responseProcessor,
        Func<TResult, string> resultFormatter,
        CancellationToken cancellationToken)
    {
        var activityId = $"prompt-{Interlocked.Increment(ref s_promptCounter)}";
        var serializedPromptData = JsonSerializer.Serialize(promptData);

        // Create the activity
        var activity = await activityReporter.CreateActivityAsync(activityId, promptText, isPrimary: false, cancellationToken).ConfigureAwait(false);

        try
        {
            // Send the prompt request
            await activityReporter.UpdateActivityStatusAsync(
                activity,
                status => status with
                {
                    StatusText = promptText,
                    IsComplete = false,
                    IsError = false,
                    PromptType = promptType.ToString(),
                    PromptData = serializedPromptData
                },
                cancellationToken).ConfigureAwait(false);

            // Wait for the response from the CLI
            var response = await activityReporter.WaitForPromptResponseAsync(activityId, cancellationToken).ConfigureAwait(false);

            // Process the response
            var result = responseProcessor(response);

            // Mark the activity as complete
            await activityReporter.UpdateActivityStatusAsync(
                activity,
                status => status with
                {
                    StatusText = resultFormatter(result),
                    IsComplete = true,
                    IsError = false,
                    PromptType = null,
                    PromptData = null
                },
                cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            // Mark the activity as failed
            await activityReporter.UpdateActivityStatusAsync(
                activity,
                status => status with
                {
                    StatusText = $"Prompt failed: {ex.Message}",
                    IsComplete = true,
                    IsError = true,
                    PromptType = null,
                    PromptData = null
                },
                cancellationToken).ConfigureAwait(false);

            throw;
        }
    }
}
