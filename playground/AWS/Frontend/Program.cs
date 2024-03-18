// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Amazon.SimpleNotificationService;
using Amazon.SQS;
using Frontend.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddAWSService<IAmazonSQS>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

// Configuring messaging using the AWS.Messaging library.
builder.Services.AddAWSMessageBus(messageBuilder =>
{
    // Get the SQS queue URL that was created from AppHost and assigned to the project.
    var chatTopicArn = builder.Configuration["AWS:Resources:ChatTopicArn"];
    if (chatTopicArn != null)
    {
        messageBuilder.AddSNSPublisher<Frontend.Models.ChatMessage>(chatTopicArn);
    }
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

app.MapDefaultEndpoints();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
