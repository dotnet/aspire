// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

internal static class InteractionCommands
{
    public static IResourceBuilder<T> AddInteractionCommands<T>(this IResourceBuilder<T> resource) where T : IResource
    {
        resource
            .WithCommand("confirmation-interaction", "Confirmation interactions", executeCommand: async commandContext =>
           {
               var interactionService = commandContext.ServiceProvider.GetRequiredService<IInteractionService>();
               var resultTask1 = interactionService.PromptConfirmationAsync("Command confirmation", "Are you sure?", cancellationToken: commandContext.CancellationToken);
               var resultTask2 = interactionService.PromptMessageBoxAsync("Command confirmation", "Are you really sure?", new MessageBoxInteractionOptions { Intent = MessageIntent.Warning, ShowSecondaryButton = true }, cancellationToken: commandContext.CancellationToken);

               await Task.WhenAll(resultTask1, resultTask2);

               if (resultTask1.Result.Data != true || resultTask2.Result.Data != true)
               {
                   return CommandResults.Failure("Canceled");
               }

               _ = interactionService.PromptMessageBoxAsync("Command executed", "The command successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success, PrimaryButtonText = "Yeah!" });
               return CommandResults.Success();
           })
           .WithCommand("messagebar-interaction", "Messagebar interactions", executeCommand: async commandContext =>
           {
               await Task.Yield();

               var interactionService = commandContext.ServiceProvider.GetRequiredService<IInteractionService>();
               _ = interactionService.PromptMessageBarAsync("Success bar", "The command successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Success });
               _ = interactionService.PromptMessageBarAsync("Information bar", "The command successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Information });
               _ = interactionService.PromptMessageBarAsync("Warning bar", "The command successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Warning });
               _ = interactionService.PromptMessageBarAsync("Error bar", "The command successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Error, LinkText = "Click here for more information", LinkUrl = "https://www.microsoft.com" });
               _ = interactionService.PromptMessageBarAsync("Confirmation bar", "The command successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Confirmation });
               _ = interactionService.PromptMessageBarAsync("No dismiss", "The command successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Information, ShowDismiss = false });

               return CommandResults.Success();
           })
           .WithCommand("html-interaction", "HTML interactions", executeCommand: async commandContext =>
           {
               var interactionService = commandContext.ServiceProvider.GetRequiredService<IInteractionService>();

               _ = interactionService.PromptMessageBarAsync("Success <strong>bar</strong>", "The **command** successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Success });
               _ = interactionService.PromptMessageBarAsync("Success <strong>bar</strong>", "The **command** successfully executed.", new MessageBarInteractionOptions { Intent = MessageIntent.Success, MessageAsMarkdown = true });

               _ = interactionService.PromptMessageBoxAsync("Success <strong>bar</strong>", "The **command** successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success });
               _ = interactionService.PromptMessageBoxAsync("Success <strong>bar</strong>", "The **command** successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success, MessageAsMarkdown = true });

               _ = await interactionService.PromptInputAsync("Text <strong>request</strong>", "Provide **your** name", "<strong>Name</strong>", "Enter <strong>your</strong> name");
               _ = await interactionService.PromptInputAsync("Text <strong>request</strong>", "Provide **your** name", "<strong>Name</strong>", "Enter <strong>your</strong> name", new InputsDialogInteractionOptions { MessageAsMarkdown = true });

               return CommandResults.Success();
           })
           .WithCommand("value-interaction", "Value interactions", executeCommand: async commandContext =>
           {
               var interactionService = commandContext.ServiceProvider.GetRequiredService<IInteractionService>();
               var result = await interactionService.PromptInputAsync(
                   title: "Text request",
                   message: "Provide your name",
                   inputLabel: "Name",
                   placeHolder: "Enter your name",
                   options: new InputsDialogInteractionOptions
                   {
                       ValidationCallback = context =>
                       {
                           var input = context.Inputs[0];
                           if (!string.IsNullOrEmpty(input.Value) && input.Value.Length < 3)
                           {
                               context.AddValidationError(input, "Name must be at least 3 characters long.");
                           }
                           return Task.CompletedTask;
                       }
                   },
                   cancellationToken: commandContext.CancellationToken);

               if (result.Canceled)
               {
                   return CommandResults.Failure("Canceled");
               }

               var resourceLoggerService = commandContext.ServiceProvider.GetRequiredService<ResourceLoggerService>();
               var logger = resourceLoggerService.GetLogger(commandContext.ResourceName);

               var input = result.Data;
               logger.LogInformation("Input: {Label} = {Value}", input.Label, input.Value);

               return CommandResults.Success();
           })
           .WithCommand("input-interaction", "Input interactions", executeCommand: async commandContext =>
           {
               var interactionService = commandContext.ServiceProvider.GetRequiredService<IInteractionService>();
               var dinnerInput = new InteractionInput
               {
                   InputType = InputType.Choice,
                   Label = "Dinner",
                   Placeholder = "Select dinner",
                   Required = true,
                   Options =
                   [
                       KeyValuePair.Create("pizza", "Pizza"),
                       KeyValuePair.Create("fried-chicken", "Fried chicken"),
                       KeyValuePair.Create("burger", "Burger"),
                       KeyValuePair.Create("salmon", "Salmon"),
                       KeyValuePair.Create("chicken-pie", "Chicken pie"),
                       KeyValuePair.Create("sushi", "Sushi"),
                       KeyValuePair.Create("tacos", "Tacos"),
                       KeyValuePair.Create("pasta", "Pasta"),
                       KeyValuePair.Create("salad", "Salad"),
                       KeyValuePair.Create("steak", "Steak"),
                       KeyValuePair.Create("vegetarian", "Vegetarian"),
                       KeyValuePair.Create("sausage", "Sausage"),
                       KeyValuePair.Create("lasagne", "Lasagne"),
                       KeyValuePair.Create("fish-pie", "Fish pie"),
                       KeyValuePair.Create("soup", "Soup"),
                       KeyValuePair.Create("beef-stew", "Beef stew"),
                   ]
               };
               var numberOfPeopleInput = new InteractionInput { InputType = InputType.Number, Label = "Number of people", Placeholder = "Enter number of people", Value = "2", Required = true };
               var inputs = new List<InteractionInput>
               {
                   new InteractionInput { InputType = InputType.Text, Label = "Name", Placeholder = "Enter name", Required = true },
                   new InteractionInput { InputType = InputType.SecretText, Label = "Password", Placeholder = "Enter password", Required = true },
                   dinnerInput,
                   numberOfPeopleInput,
                   new InteractionInput { InputType = InputType.Boolean, Label = "Remember me", Placeholder = "What does this do?", Required = true },
               };
               var result = await interactionService.PromptInputsAsync(
                   "Input request",
                   "Provide your name",
                   inputs,
                   options: new InputsDialogInteractionOptions
                   {
                       ValidationCallback = context =>
                       {
                           if (dinnerInput.Value == "steak" && int.TryParse(numberOfPeopleInput.Value, CultureInfo.InvariantCulture, out var i) && i > 4)
                           {
                               context.AddValidationError(numberOfPeopleInput, "Number of people can't be greater than 4 when eating steak.");
                           }
                           return Task.CompletedTask;
                       }
                   },
                   cancellationToken: commandContext.CancellationToken);

               if (result.Canceled)
               {
                   return CommandResults.Failure("Canceled");
               }

               var resourceLoggerService = commandContext.ServiceProvider.GetRequiredService<ResourceLoggerService>();
               var logger = resourceLoggerService.GetLogger(commandContext.ResourceName);

               foreach (var updatedInput in result.Data)
               {
                   logger.LogInformation("Input: {Label} = {Value}", updatedInput.Label, updatedInput.Value);
               }

               return CommandResults.Success();
           })
           .WithCommand("many-values", "Many values", executeCommand: async commandContext =>
           {
               var interactionService = commandContext.ServiceProvider.GetRequiredService<IInteractionService>();
               var inputs = new List<InteractionInput>();
               for (var i = 0; i < 50; i++)
               {
                   inputs.Add(new InteractionInput
                   {
                       InputType = InputType.Text,
                       Label = $"Input {i + 1}",
                       Placeholder = $"Enter input {i + 1}"
                   });
               }
               var result = await interactionService.PromptInputsAsync(
                   title: "Text request",
                   message: "Provide your name",
                   inputs: inputs,
                   cancellationToken: commandContext.CancellationToken);

               if (result.Canceled)
               {
                   return CommandResults.Failure("Canceled");
               }

               var resourceLoggerService = commandContext.ServiceProvider.GetRequiredService<ResourceLoggerService>();
               var logger = resourceLoggerService.GetLogger(commandContext.ResourceName);

               foreach (var input in result.Data)
               {
                   logger.LogInformation("Input: {Label} = {Value}", input.Label, input.Value);
               }

               return CommandResults.Success();
           });

        return resource;
    }
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
