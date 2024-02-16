using OrderProcessor;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddRabbitMQ("messaging");
builder.Services.AddHostedService<OrderProcessingWorker>();

var host = builder.Build();
host.Run();
