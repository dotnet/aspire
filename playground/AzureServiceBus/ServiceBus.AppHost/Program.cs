using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.ServiceBus;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("sbemulator");

serviceBus
    .WithQueue("myQueue", queue =>
    {
        queue.DeadLetteringOnMessageExpiration = false;
        queue.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
        queue.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
        queue.ForwardDeadLetteredMessagesTo = "";
        queue.LockDuration = TimeSpan.FromMinutes(1);
        queue.MaxDeliveryCount = 10;
        queue.RequiresDuplicateDetection = false;
        queue.RequiresSession = false;
    })
    .WithTopic("myTopic", topic =>
    {
        topic.Name = "topic.1";
        topic.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
        topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
        topic.RequiresDuplicateDetection = false;

        var subscription = new ServiceBusSubscription("mySubscription")
        {
            Name = "subscription.1",
            DeadLetteringOnMessageExpiration = false,
            DefaultMessageTimeToLive = TimeSpan.FromHours(1),
            LockDuration = TimeSpan.FromMinutes(1),
            MaxDeliveryCount = 10,
            ForwardDeadLetteredMessagesTo = "",
            RequiresSession = false
        };
        topic.Subscriptions.Add(subscription);

        var rule = new ServiceBusRule("app-prop-filter-1")
        {
            CorrelationFilter = new()
            {
                ContentType = "application/text",
                CorrelationId = "id1",
                Subject = "subject1",
                MessageId = "msgid1",
                ReplyTo = "someQueue",
                ReplyToSessionId = "sessionId",
                SessionId = "session1",
                SendTo = "xyz"
            }
        };
        subscription.Rules.Add(rule);
    })
    ;

serviceBus.RunAsEmulator(configure => configure.ConfigureJson(document =>
{
    document["UserConfig"]!["Logging"] = new JsonObject { ["Type"] = "Console" };
}));

builder.AddProject<Projects.ServiceBusWorker>("worker")
    .WithReference(serviceBus).WaitFor(serviceBus);

builder.Build().Run();
