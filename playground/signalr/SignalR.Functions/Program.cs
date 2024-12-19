using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = FunctionsApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureQueueClient("queue");
builder.AddAzureBlobClient("blob");

builder.ConfigureFunctionsWebApplication();

builder.Services.AddOptions<KestrelServerOptions>()
 .Configure<IConfiguration>((settings, configuration) =>
 {
     settings.AllowSynchronousIO = true;
     configuration.Bind(settings);
 });

var host = builder.Build();

host.Run();
