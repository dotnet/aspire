using Aspire.Hosting.Azure;

var builder = DistributedApplication.CreateBuilder(args);

var scheduler =
    builder.AddDurableTaskScheduler("scheduler")
           .RunAsExisting(builder.AddParameter("scheduler-connection-string"));

var taskHub =
    scheduler.AddTaskHub("taskhub")
             .WithTaskHubName(builder.AddParameter("taskhub-name"));

var webApi =
    builder.AddProject<Projects.DurableTask_Scheduler_WebApi>("webapi")
           .WithReference(taskHub);

builder.AddProject<Projects.DurableTask_Scheduler_Worker>("worker")
       .WithReference(webApi)
       .WithReference(taskHub);

builder.Build().Run();
