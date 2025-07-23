using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var scheduler =
    builder.AddDurableTaskScheduler("scheduler")
           .RunAsEmulator(
                options =>
                {
                    options.WithDynamicTaskHubs();
                });

var taskHub = scheduler.AddTaskHub("taskhub");

var webApi =
    builder.AddProject<Projects.DurableTask_Scheduler_WebApi>("webapi")
           .WithReference(taskHub);

builder.AddProject<Projects.DurableTask_Scheduler_Worker>("worker")
       .WithReference(webApi)
       .WithReference(taskHub);

builder.Build().Run();
