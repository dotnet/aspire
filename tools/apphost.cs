// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable ASPIREPIPELINES001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIRECSHARPAPPS001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
#pragma warning disable ASPIREINTERACTION001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

#:property ManagePackageVersionsCentrally=false
#:sdk Aspire.AppHost.Sdk@13.1.0-preview.1.25603.4

#:package System.Net.Http.Json@9.0.0

using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

var builder = DistributedApplication.CreateBuilder(args);

builder.AddCSharpApp("investigate-test", "./scripts/DownloadFailingJobLogs.cs")
       .WithArgs(async context =>
       {
           var interactions = context.ExecutionContext.ServiceProvider.GetRequiredService<IInteractionService>();
           var ct = context.CancellationToken;

           // Try to get GitHub token from gh CLI first
           string? githubToken = null;
           try
           {
               var psi = new ProcessStartInfo("gh", "auth token")
               {
                   RedirectStandardOutput = true,
                   RedirectStandardError = true,
                   UseShellExecute = false
               };
               using var process = Process.Start(psi);
               if (process is not null)
               {
                   githubToken = (await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false)).Trim();
                   await process.WaitForExitAsync(ct).ConfigureAwait(false);
                   if (process.ExitCode != 0 || string.IsNullOrEmpty(githubToken))
                   {
                       githubToken = null;
                   }
               }
           }
           catch
           {
               // gh CLI not available or failed
           }

           // If gh auth token failed, prompt for token
           if (string.IsNullOrEmpty(githubToken))
           {
               var tokenResult = await interactions.PromptInputAsync(
                   "GitHub Authentication",
                   "Could not get token from `gh auth token`. Please enter your GitHub token manually:",
                   new InteractionInput
                   {
                       Name = "github_token",
                       Label = "GitHub Token",
                       InputType = InputType.SecretText,
                       Required = true,
                       Placeholder = "ghp_xxxxxxxxxxxx"
                   },
                   cancellationToken: ct).ConfigureAwait(false);

               if (tokenResult.Canceled || string.IsNullOrEmpty(tokenResult.Data?.Value))
               {
                   return;
               }

               githubToken = tokenResult.Data.Value;
           }

           // Create HTTP client for GitHub API
           using var httpClient = new HttpClient();
           httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
           httpClient.DefaultRequestHeaders.Add("User-Agent", "Aspire-Job-Analyzer");
           httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
           httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

           // Single form with both branch and workflow run selection using dynamic loading
           var result = await interactions.PromptInputsAsync(
               "Select Failed Workflow Run",
               "Choose a branch and failed workflow run to analyze:",
               [
                   new InteractionInput
                   {
                       Name = "Branch",
                       InputType = InputType.Choice,
                       Label = "Branch",
                       Required = false,
                       DynamicLoading = new InputLoadOptions
                       {
                           LoadCallback = async (loadContext) =>
                           {
                               var runsResponse = await httpClient.GetFromJsonAsync<WorkflowRunsResponse>(
                                   "https://api.github.com/repos/dotnet/aspire/actions/workflows/ci.yml/runs?status=failure&per_page=50",
                                   loadContext.CancellationToken).ConfigureAwait(false);

                               var branches = runsResponse?.WorkflowRuns?
                                   .Select(r => r.HeadBranch)
                                   .Distinct()
                                   .OrderBy(b => b)
                                   .Select(b => KeyValuePair.Create(b, b))
                                   .ToList() ?? [];

                               branches.Insert(0, KeyValuePair.Create("", "All branches"));
                               loadContext.Input.Options = branches;
                           }
                       }
                   },
                   new InteractionInput
                   {
                       Name = "WorkflowRun",
                       InputType = InputType.Choice,
                       Label = "Failed Workflow Run",
                       Required = true,
                       DynamicLoading = new InputLoadOptions
                       {
                           DependsOnInputs = ["Branch"],
                           LoadCallback = async (loadContext) =>
                           {
                               var branch = loadContext.AllInputs["Branch"].Value;
                               var url = string.IsNullOrEmpty(branch)
                                   ? "https://api.github.com/repos/dotnet/aspire/actions/workflows/ci.yml/runs?status=failure&per_page=20"
                                   : $"https://api.github.com/repos/dotnet/aspire/actions/workflows/ci.yml/runs?status=failure&branch={Uri.EscapeDataString(branch)}&per_page=20";

                               var runs = await httpClient.GetFromJsonAsync<WorkflowRunsResponse>(url, loadContext.CancellationToken).ConfigureAwait(false);

                               loadContext.Input.Options = runs?.WorkflowRuns?
                                   .Select(r => KeyValuePair.Create(
                                       r.Id.ToString(),
                                       $"#{r.RunNumber} - {r.DisplayTitle} ({r.HeadBranch}) - {r.CreatedAt:g}"))
                                   .ToList() ?? [];
                           }
                       }
                   }
               ],
               cancellationToken: ct).ConfigureAwait(false);

           if (result.Canceled || string.IsNullOrEmpty(result.Data?["WorkflowRun"]?.Value))
           {
               return;
           }

           // Pass the workflow run ID to the script - it will find the failed jobs
           context.Args.Add(result.Data["WorkflowRun"].Value);
       })
       .WithExplicitStart();


builder.Pipeline.AddStep("investigate-test", async (context) =>
{
    var interactions = context.ExecutionContext.ServiceProvider.GetRequiredService<IInteractionService>();
    var ct = context.CancellationToken;

    // Try to get GitHub token from gh CLI first
    string? githubToken = null;
    try
    {
        var psi = new ProcessStartInfo("gh", "auth token")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        using var process = Process.Start(psi);
        if (process is not null)
        {
            githubToken = (await process.StandardOutput.ReadToEndAsync(ct).ConfigureAwait(false)).Trim();
            await process.WaitForExitAsync(ct).ConfigureAwait(false);
            if (process.ExitCode != 0 || string.IsNullOrEmpty(githubToken))
            {
                githubToken = null;
            }
        }
    }
    catch
    {
        // gh CLI not available or failed
    }

    // If gh auth token failed, prompt for token
    if (string.IsNullOrEmpty(githubToken))
    {
        var tokenResult = await interactions.PromptInputAsync(
            "GitHub Authentication",
            "Could not get token from `gh auth token`. Please enter your GitHub token manually:",
            new InteractionInput
            {
                Name = "github_token",
                Label = "GitHub Token",
                InputType = InputType.SecretText,
                Required = true,
                Placeholder = "ghp_xxxxxxxxxxxx"
            },
            cancellationToken: ct).ConfigureAwait(false);

        if (tokenResult.Canceled || string.IsNullOrEmpty(tokenResult.Data?.Value))
        {
            return;
        }

        githubToken = tokenResult.Data.Value;
    }

    // Create HTTP client for GitHub API
    using var httpClient = new HttpClient();
    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {githubToken}");
    httpClient.DefaultRequestHeaders.Add("User-Agent", "Aspire-Job-Analyzer");
    httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github+json");
    httpClient.DefaultRequestHeaders.Add("X-GitHub-Api-Version", "2022-11-28");

    var runsResponse = await httpClient.GetFromJsonAsync<WorkflowRunsResponse>(
                        "https://api.github.com/repos/dotnet/aspire/actions/workflows/ci.yml/runs?status=failure&per_page=20",
                        ct).ConfigureAwait(false);

    // Get unique branches from the failed runs
    var branches = runsResponse?.WorkflowRuns?
        .Select(r => r.HeadBranch)
        .Distinct()
        .OrderBy(b => b)
        .Select(b => KeyValuePair.Create(b, b))
        .ToList() ?? [];

    // Insert "All branches" option at the start
    branches.Insert(0, KeyValuePair.Create("", "All branches"));

    // First prompt: select a branch
    var branchResult = await interactions.PromptInputAsync(
        "Select Branch",
        "Choose a branch to filter failed workflow runs:",
        new InteractionInput
        {
            Name = "Branch",
            InputType = InputType.Choice,
            Label = "Branch",
            Required = false,
            Options = branches
        },
        cancellationToken: ct).ConfigureAwait(false);

    if (branchResult.Canceled)
    {
        return;
    }

    // Fetch workflow runs filtered by branch if selected
    var selectedBranch = branchResult.Data?.Value;
    var runsUrl = string.IsNullOrEmpty(selectedBranch)
        ? "https://api.github.com/repos/dotnet/aspire/actions/workflows/ci.yml/runs?status=failure&per_page=20"
        : $"https://api.github.com/repos/dotnet/aspire/actions/workflows/ci.yml/runs?status=failure&branch={Uri.EscapeDataString(selectedBranch)}&per_page=20";

    var filteredRunsResponse = await httpClient.GetFromJsonAsync<WorkflowRunsResponse>(runsUrl, ct).ConfigureAwait(false);

    var runOptions = filteredRunsResponse?.WorkflowRuns?
        .Select(r => KeyValuePair.Create(
            r.Id.ToString(),
            $"#{r.RunNumber} - {r.DisplayTitle} ({r.HeadBranch}) - {r.CreatedAt:g}"))
        .ToList() ?? [];

    if (runOptions.Count == 0)
    {
        await interactions.PromptNotificationAsync(
            "No Failed Runs",
            "No failed workflow runs found for the selected branch.",
            new NotificationInteractionOptions { Intent = MessageIntent.Information },
            ct).ConfigureAwait(false);
        return;
    }

    // Second prompt: select a workflow run
    var result = await interactions.PromptInputAsync(
        "Select Failed Workflow Run",
        "Choose a failed workflow run to analyze:",
        new InteractionInput
        {
            Name = "WorkflowRun",
            InputType = InputType.Choice,
            Label = "Failed Workflow Run",
            Required = true,
            Options = runOptions
        },
        cancellationToken: ct).ConfigureAwait(false);

    if (result.Canceled || string.IsNullOrEmpty(result.Data?.Value))
    {
        return;
    }

    // Execute dotnet scripts/DownloadFailingJobLogs.cs with the selected workflow run ID
    // and log the output to context.Logger as debug information
    var psiScript = new ProcessStartInfo("dotnet", $"scripts/DownloadFailingJobLogs.cs {result.Data.Value}")
    {
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false
    };
    var processScript = Process.Start(psiScript);

    if (processScript is null)
    {
        context.Logger.LogError("Failed to start DownloadFailingJobLogs.cs script process.");
        return;
    }

    processScript.BeginErrorReadLine();
    processScript.BeginOutputReadLine();

    processScript.OutputDataReceived += (sender, e) =>
    {
        if (e.Data is not null)
        {
            context.Logger.LogInformation("{Output}", e.Data);
        }
    };

    processScript.ErrorDataReceived += (sender, e) =>
    {
        if (e.Data is not null)
        {
            context.Logger.LogInformation("{Error}", e.Data);
        }
    };

    // We want to stream the logs

    await processScript.WaitForExitAsync(ct).ConfigureAwait(false);
});

builder.Build().Run();

// GitHub API response models
internal record WorkflowRunsResponse(
    [property: JsonPropertyName("workflow_runs")] List<WorkflowRun> WorkflowRuns);

internal record WorkflowRun(
    [property: JsonPropertyName("id")] long Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("display_title")] string DisplayTitle,
    [property: JsonPropertyName("run_number")] int RunNumber,
    [property: JsonPropertyName("head_branch")] string HeadBranch,
    [property: JsonPropertyName("created_at")] DateTime CreatedAt);
