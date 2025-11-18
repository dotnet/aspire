using Azure.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

var storage = builder.AddAzureStorage("storage").RunAsEmulator();

var dts = builder.AddDurableTaskScheduler("dts").RunAsEmulator();

var taskHub = dts.AddTaskHub("default");

builder.AddAzureFunctionsProject<Projects.AzureFunctionsWithDts_Functions>("funcapp")
    .WithHostStorage(storage)
    .WaitFor(dts)
    .WithEnvironment("DURABLE_TASK_SCHEDULER_CONNECTION_STRING", dts)
    .WithEnvironment("TASKHUB_NAME", taskHub.Resource.Name);

builder.Build().Run();
