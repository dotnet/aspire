var builder = DistributedApplication.CreateBuilder(args);

var serviceBus = builder.AddAzureServiceBus("sbemulator")
    .RunAsEmulator() // Comment to deploy and use the Azure cloud
    ;

serviceBus
    .AddQueue("myQueue", queue =>
    {
        queue.Name = "queue.1";
        queue.DeadLetteringOnMessageExpiration = false;
        queue.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
        queue.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
        queue.ForwardDeadLetteredMessagesTo = "";
        queue.LockDuration = TimeSpan.FromMinutes(1);
        queue.MaxDeliveryCount = 10;
        queue.RequiresDuplicateDetection = false;
        queue.RequiresSession = false;
    })
    //.AddTopic("myTopic", topic =>
    //{
    //    topic.Name = "topic.1";
    //    topic.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
    //    topic.DuplicateDetectionHistoryTimeWindow = TimeSpan.FromSeconds(20);
    //    topic.RequiresDuplicateDetection = false;
    //})
    //.AddSubscription("myTopic", "mySubscription", sub =>
    //{
    //    sub.Name = "subscription.1";
    //    sub.DeadLetteringOnMessageExpiration = false;
    //    sub.DefaultMessageTimeToLive = TimeSpan.FromHours(1);
    //    sub.LockDuration = TimeSpan.FromMinutes(1);
    //    sub.MaxDeliveryCount = 10;
    //    sub.ForwardDeadLetteredMessagesTo = "";
    //    sub.RequiresSession = false;
    //})
    //.AddRule("myTopic", "mySubscription", "myRule", rule =>
    //{
    //    rule.Name = "app-prop-filter-1";
    //    rule.CorrelationFilter = new()
    //    {
    //        ContentType = "application/text",
    //        CorrelationId = "id1",
    //        Subject = "subject1",
    //        MessageId = "msgid1",
    //        ReplyTo = "someQueue",
    //        ReplyToSessionId = "sessionId",
    //        SessionId = "session1",
    //        SendTo = "xyz"
    //    };
    //})
    ;

builder.AddProject<Projects.ServiceBusWorker>("worker")
    .WithReference(serviceBus).WaitFor(serviceBus);

builder.Build().Run();
