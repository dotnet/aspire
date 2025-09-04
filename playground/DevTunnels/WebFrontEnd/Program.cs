// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using WebFrontEnd.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient("gateway", client => client.BaseAddress = new Uri("https+http://gateway"));

builder.Services.AddHttpClient("nuget", client => client.BaseAddress = new Uri("https://nuget"));

builder.Services.AddHttpClient("external-service", client =>
{
    // The URL is set in appsettings.json or can be overridden by an environment variable
    client.BaseAddress = new Uri(builder.Configuration["EXTERNAL_SERVICE_URL"]
        ?? throw new InvalidOperationException("Missing URL for exteral service"));
});

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

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapDefaultEndpoints();

app.Run();
