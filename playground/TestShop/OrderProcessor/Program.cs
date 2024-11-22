using OrderProcessor;

System.Diagnostics.Debugger.Launch();

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQClient("messaging");
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
