// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using GitHubModelsEndToEnd.WebStory.Components;
using Microsoft.Extensions.AI;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddOpenTelemetry().WithTracing(b => b.AddSource("Experimental.Microsoft.Extensions.AI"));
builder.Services.AddOpenTelemetry().WithMetrics(b => b.AddMeter("Experimental.Microsoft.Extensions.AI"));

builder.AddAzureChatCompletionsClient("chat")
       .AddChatClient()
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
