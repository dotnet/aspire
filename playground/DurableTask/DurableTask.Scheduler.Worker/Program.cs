using DurableTask.Scheduler.Worker.Tasks.Echo;
using Microsoft.DurableTask.Worker;
using Microsoft.DurableTask.Worker.AzureManaged;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddServiceDiscovery();

builder
    .Services
    .AddHttpClient("Echo",
        client => client.BaseAddress = new Uri("https+http://webapi"))
    .AddServiceDiscovery();

builder.Services.AddDurableTaskWorker(
    workerBuilder =>
    {
        workerBuilder.AddTasks(r =>
        {
            r.AddActivity<EchoActivity>("EchoActivity");
            r.AddOrchestrator<EchoOrchestrator>("EchoOrchestrator");
        });
        workerBuilder.UseDurableTaskScheduler(
            builder.Configuration.GetConnectionString("taskhub") ?? throw new InvalidOperationException("Scheduler connection string not configured."),
            options =>
            {
                options.AllowInsecureCredentials = true;
            });
    });

var host = builder.Build();

host.Run();
