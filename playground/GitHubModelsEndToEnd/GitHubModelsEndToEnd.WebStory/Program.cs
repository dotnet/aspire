// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using GitHubModelsEndToEnd.WebStory.Components;
using Microsoft.Extensions.AI;
using OpenTelemetry;
using OpenTelemetry.Trace;

AppContext.SetSwitch("Azure.Experimental.TraceGenAIMessageContent", true);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry().WithTracing(b => b.AddSource("Experimental.Microsoft.Extensions.AI"));
builder.Services.AddOpenTelemetry().WithTracing(b => b.AddSource("WebStory"));
builder.Services.AddOpenTelemetry().WithMetrics(b => b.AddMeter("Experimental.Microsoft.Extensions.AI"));

builder.Services.AddOpenTelemetry().WithTracing(t => t.AddProcessor(new ActivityFilteringProcessor(activity =>
{
    if (activity.Source.Name.StartsWith("Azure."))
    {
        return false;
    }
    return true;
}))).UseAzureMonitor();

builder.AddAzureChatCompletionsClient("chat", s => s.DisableTracing = true)
       .AddChatClient(deploymentId: null, configureChatClient: c => c.EnableSensitiveData = true)
       .UseFunctionInvocation();
       //.UseOpenTelemetry(configure: c => c.EnableSensitiveData = true);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();

sealed class ActivityFilteringProcessor : BaseProcessor<Activity>
{
    private readonly Func<Activity, bool> _shouldKeep;

    public ActivityFilteringProcessor(Func<Activity, bool> shouldKeep) =>
        _shouldKeep = shouldKeep;

    public override void OnStart(Activity data)
    {
        if (!_shouldKeep(data))
        {
            data.IsAllDataRequested = false; // disables enrichment
            data.ActivityTraceFlags &= ~ActivityTraceFlags.Recorded; // marks as not recorded
        }
    }
}
