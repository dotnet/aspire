using System.Text.Json.Nodes;
using Aspire.Hosting.Azure.ServiceBus;

var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("sbemulator");

serviceBus
    .WithQueue("queue1", queue =>
    {
        queue.DeadLetteringOnMessageExpiration = false;
    })
    .WithTopic("topic1", topic =>
    {
        var subscription = new ServiceBusSubscription("sub1")
        {
            MaxDeliveryCount = 10,
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

serviceBus.RunAsEmulator(configure => configure.ConfigureEmulator(document =>
{
    document["UserConfig"]!["Logging"] = new JsonObject { ["Type"] = "Console" };
}));

builder.AddProject<Projects.ServiceBusWorker>("worker")
    .WithReference(serviceBus).WaitFor(serviceBus);

builder.Build().Run();
