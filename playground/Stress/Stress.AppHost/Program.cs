// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var builder = DistributedApplication.CreateBuilder(args);
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks().AddAsyncCheck("health-test", async (ct) =>
{
    await Task.Delay(5_000, ct);
    return HealthCheckResult.Healthy();
});

for (var i = 0; i < 5; i++)
{
    var name = $"test-{i:0000}";
    var rb = builder.AddTestResource(name);
    IResource parent = rb.Resource;

    for (var j = 0; j < 3; j++)
    {
        name += $"-n{j}";
        var nestedRb = builder.AddNestedResource(name, parent);
        parent = nestedRb.Resource;
    }
}

builder.AddParameter("testParameterResource", () => "value", secret: true);
builder.AddContainer("hiddenContainer", "alpine")
    .WithInitialState(new CustomResourceSnapshot
    {
        ResourceType = "CustomHiddenContainerType",
        Properties = [],
        IsHidden = true
    });

// TODO: OTEL env var can be removed when OTEL libraries are updated to 1.9.0
// See https://github.com/open-telemetry/opentelemetry-dotnet/blob/main/RELEASENOTES.md#1100
var serviceBuilder = builder.AddProject<Projects.Stress_ApiService>("stress-apiservice", launchProfileName: null)
    .WithEnvironment("OTEL_DOTNET_EXPERIMENTAL_METRICS_EMIT_OVERFLOW_ATTRIBUTE", "true");
serviceBuilder
    .WithEnvironment("HOST", $"{serviceBuilder.GetEndpoint("http").Property(EndpointProperty.Host)}")
    .WithEnvironment("PORT", $"{serviceBuilder.GetEndpoint("http").Property(EndpointProperty.Port)}")
    .WithEnvironment("URL", $"{serviceBuilder.GetEndpoint("http").Property(EndpointProperty.Url)}");
serviceBuilder.WithCommand(
    name: "icon-test",
    displayName: "Icon test",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    commandOptions: new CommandOptions
    {
        IconName = "CloudDatabase"
    });
serviceBuilder.WithCommand(
    name: "icon-test-highlighted",
    displayName: "Icon test highlighted",
    executeCommand: (c) =>
    {
        return Task.FromResult(CommandResults.Success());
    },
    commandOptions: new CommandOptions
    {
        IconName = "CloudDatabase",
        IsHighlighted = true
    });

serviceBuilder.WithHttpEndpoint(5180, name: $"http");
for (var i = 1; i <= 30; i++)
{
    var port = 5180 + i;
    serviceBuilder.WithHttpEndpoint(port, name: $"http-{port}");
}

serviceBuilder.WithHttpCommand("/write-console", "Write to console", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/increment-counter", "Increment counter", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/big-trace", "Big trace", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/trace-limit", "Trace limit", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/log-message", "Log message", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/log-message-limit", "Log message limit", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/multiple-traces-linked", "Multiple traces linked", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/overflow-counter", "Overflow counter", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });
serviceBuilder.WithHttpCommand("/nested-trace-spans", "Out of order nested spans", commandOptions: new() { Method = HttpMethod.Get, IconName = "ContentViewGalleryLightning" });

builder.AddProject<Projects.Stress_TelemetryService>("stress-telemetryservice")
       .WithUrls(c => c.Urls.Add(new() { Url = "https://someplace.com", DisplayText = "Some place" }))
       .WithUrl("https://someotherplace.com/some-path", "Some other place")
       .WithUrl("https://extremely-long-url.com/abcdefghijklmnopqrstuvwxyz/abcdefghijklmnopqrstuvwxyz/abcdefghijklmnopqrstuvwxyz//abcdefghijklmnopqrstuvwxyz/abcdefghijklmnopqrstuvwxyz/abcdefghijklmnopqrstuvwxyz/abcdefghijklmnopqrstuvwxyz/abcdefghijklmno")
       .WithCommand(
           name: "long-command",
           displayName: "This is a custom command with a very long command display name",
           executeCommand: (c) =>
           {
               return Task.FromResult(CommandResults.Success());
           },
           commandOptions: new() { IconName = "CloudDatabase" })
       .WithCommand(
           name: "resource-stop-all",
           displayName: "Stop all resources",
           executeCommand: async (c) =>
           {
               await ExecuteCommandForAllResourcesAsync(c.ServiceProvider, "resource-stop", c.CancellationToken);
               return CommandResults.Success();
           },
           commandOptions: new() { IconName = "Stop", IconVariant = IconVariant.Filled })
       .WithCommand(
           name: "resource-start-all",
           displayName: "Start all resources",
           executeCommand: async (c) =>
           {
               await ExecuteCommandForAllResourcesAsync(c.ServiceProvider, "resource-start", c.CancellationToken);
               return CommandResults.Success();
           },
           commandOptions: new() { IconName = "Play", IconVariant = IconVariant.Filled })
       .WithCommand("confirmation-interaction", "Confirmation interactions", executeCommand: async commandContext =>
       {
           var interactionService = commandContext.ServiceProvider.GetRequiredService<InteractionService>();
           var resultTask1 = interactionService.PromptConfirmationAsync("Command confirmation", "Are you sure?", cancellationToken: commandContext.CancellationToken);
           var resultTask2 = interactionService.PromptConfirmationAsync("Command confirmation", "Are you really sure?", new MessageBoxInteractionOptions { Intent = MessageIntent.Warning }, cancellationToken: commandContext.CancellationToken);

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

           var interactionService = commandContext.ServiceProvider.GetRequiredService<InteractionService>();
           _ = interactionService.PromptMessageBarAsync("Success bar", "The command successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success });
           _ = interactionService.PromptMessageBarAsync("Information bar", "The command successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Information });
           _ = interactionService.PromptMessageBarAsync("Warning bar", "The command successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Warning });
           _ = interactionService.PromptMessageBarAsync("Error bar", "The command successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Error });
           _ = interactionService.PromptMessageBarAsync("Confirmation bar", "The command successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Confirmation });
           _ = interactionService.PromptMessageBarAsync("No dismiss", "The command successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Information, ShowDismiss = false });

           return CommandResults.Success();
       })
       .WithCommand("html-interaction", "HTML interactions", executeCommand: async commandContext =>
       {
           var interactionService = commandContext.ServiceProvider.GetRequiredService<InteractionService>();

           _ = interactionService.PromptMessageBarAsync("Success <strong>bar</strong>", "The <strong>command</strong> successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success });
           _ = interactionService.PromptMessageBarAsync("Success <strong>bar</strong>", "The <strong>command</strong> successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success, EscapeMessageHtml = false });

           _ = interactionService.PromptMessageBoxAsync("Success <strong>bar</strong>", "The <strong>command</strong> successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success });
           _ = interactionService.PromptMessageBoxAsync("Success <strong>bar</strong>", "The <strong>command</strong> successfully executed.", new MessageBoxInteractionOptions { Intent = MessageIntent.Success, EscapeMessageHtml = false });

           _ = await interactionService.PromptInputAsync("Text <strong>request</strong>", "Provide <strong>your</strong> name", "<strong>Name</strong>", "Enter <strong>your</strong> name");
           _ = await interactionService.PromptInputAsync("Text <strong>request</strong>", "Provide <strong>your</strong> name", "<strong>Name</strong>", "Enter <strong>your</strong> name", new InputsDialogInteractionOptions { EscapeMessageHtml = false });

           return CommandResults.Success();
       })
       .WithCommand("value-interaction", "Value interactions", executeCommand: async commandContext =>
       {
           var interactionService = commandContext.ServiceProvider.GetRequiredService<InteractionService>();
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
           var interactionService = commandContext.ServiceProvider.GetRequiredService<InteractionService>();
           var dinnerInput = new InteractionInput
           {
               InputType = InputType.Select,
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
               new InteractionInput { InputType = InputType.Password, Label = "Password", Placeholder = "Enter password", Required = true },
               dinnerInput,
               numberOfPeopleInput,
               new InteractionInput { InputType = InputType.Checkbox, Label = "Remember me", Placeholder = "What does this do?", Required = true },
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
       });

#if !SKIP_DASHBOARD_REFERENCE
// This project is only added in playground projects to support development/debugging
// of the dashboard. It is not required in end developer code. Comment out this code
// or build with `/p:SkipDashboardReference=true`, to test end developer
// dashboard launch experience, Refer to Directory.Build.props for the path to
// the dashboard binary (defaults to the Aspire.Dashboard bin output in the
// artifacts dir).
builder.AddProject<Projects.Aspire_Dashboard>(KnownResourceNames.AspireDashboard);
#endif

IResourceBuilder<IResource>? previousResourceBuilder = null;

for (var i = 0; i < 3; i++)
{
    var resourceBuilder = builder.AddProject<Projects.Stress_Empty>($"empty-{i:0000}");
    if (previousResourceBuilder != null)
    {
        resourceBuilder.WaitFor(previousResourceBuilder);
        resourceBuilder.WithHealthCheck("health-test");
    }

    previousResourceBuilder = resourceBuilder;
}

builder.Build().Run();

static async Task ExecuteCommandForAllResourcesAsync(IServiceProvider serviceProvider, string commandName, CancellationToken cancellationToken)
{
    var commandService = serviceProvider.GetRequiredService<ResourceCommandService>();
    var model = serviceProvider.GetRequiredService<DistributedApplicationModel>();

    var resources = model.Resources
        .Where(r => r.IsContainer() || r is ProjectResource || r is ExecutableResource)
        .Where(r => r.Name != KnownResourceNames.AspireDashboard)
        .ToList();

    var commandTasks = new List<Task>();
    foreach (var r in resources)
    {
        commandTasks.Add(commandService.ExecuteCommandAsync(r, commandName, cancellationToken));
    }
    await Task.WhenAll(commandTasks).ConfigureAwait(false);
}

#pragma warning restore ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
