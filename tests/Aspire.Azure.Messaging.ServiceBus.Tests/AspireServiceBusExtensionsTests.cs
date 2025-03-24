// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Aspire.Azure.Messaging.ServiceBus.Tests;

public class AspireServiceBusExtensionsTests
{
    private const string ConnectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake";

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", "Endpoint=sb://unused.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake")
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb", settings => settings.ConnectionString = ConnectionString);
        }
        else
        {
            builder.AddAzureServiceBusClient("sb", settings => settings.ConnectionString = ConnectionString);
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "sb" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConnectionString)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NamespaceWorksInConnectionStrings(bool useKeyed)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb", ConformanceTests.FullyQualifiedNamespace)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedAzureServiceBusClient("sb");
        }
        else
        {
            builder.AddAzureServiceBusClient("sb");
        }

        using var host = builder.Build();
        var client = useKeyed ?
            host.Services.GetRequiredKeyedService<ServiceBusClient>("sb") :
            host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client.FullyQualifiedNamespace);
    }

    [Fact]
    public void CanAddMultipleKeyedServices()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:sb1", ConnectionString),
            new KeyValuePair<string, string?>("ConnectionStrings:sb2", "Endpoint=sb://aspireservicebustests2.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake"),
            new KeyValuePair<string, string?>("ConnectionStrings:sb3", "Endpoint=sb://aspireservicebustests3.servicebus.windows.net/;SharedAccessKeyName=fake;SharedAccessKey=fake")
        ]);

        builder.AddAzureServiceBusClient("sb1");
        builder.AddKeyedAzureServiceBusClient("sb2");
        builder.AddKeyedAzureServiceBusClient("sb3");

        using var host = builder.Build();

        // Unkeyed services don't work with keyed services. See https://github.com/dotnet/aspire/issues/3890
        //var client1 = host.Services.GetRequiredService<ServiceBusClient>();
        var client2 = host.Services.GetRequiredKeyedService<ServiceBusClient>("sb2");
        var client3 = host.Services.GetRequiredKeyedService<ServiceBusClient>("sb3");

        //Assert.NotSame(client1, client2);
        //Assert.NotSame(client1, client3);
        Assert.NotSame(client2, client3);

        //Assert.Equal(ConformanceTests.FullyQualifiedNamespace, client1.FullyQualifiedNamespace);
        Assert.Equal("aspireservicebustests2.servicebus.windows.net", client2.FullyQualifiedNamespace);
        Assert.Equal("aspireservicebustests3.servicebus.windows.net", client3.FullyQualifiedNamespace);
    }

    [Fact]
    public void FavorsNamedClientOptionsOverTopLevelClientOptionsWhenBothProvided()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("Aspire:Azure:Messaging:ServiceBus:ClientOptions:Identifier", "top-level-identifier"),
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", "sb", "ConnectionString"), ConnectionString),
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:Azure:Messaging:ServiceBus", "sb", "ClientOptions:Identifier"), "local-identifier"),
        ]);

        builder.AddAzureServiceBusClient("sb");

        using var host = builder.Build();

        var client = host.Services.GetRequiredService<ServiceBusClient>();
        Assert.Equal("local-identifier", client.Identifier);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=myqueue;", "aspireservicebustests.servicebus.windows.net")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=mytopic;", "aspireservicebustests.servicebus.windows.net")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=mytopic/Subscriptions/mysub;", "aspireservicebustests.servicebus.windows.net")]
    [InlineData("Endpoint=sb://localhost:50418;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true", "localhost")]
    [InlineData("Endpoint=sb://localhost:50418;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;EntityPath=myqueue", "localhost")]
    [InlineData("Endpoint=sb://localhost:50418;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;EntityPath=mytopic/Subscriptions/mysub;", "localhost")]
    [InlineData("aspireservicebustests.servicebus.windows.net", "aspireservicebustests.servicebus.windows.net")]
    public void AddAzureServiceBusClient_EnsuresConnectionStringIsCorrect(string connectionString, string expectedEndpoint)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusClient("sb");

        using var host = builder.Build();
        var client = host.Services.GetRequiredService<ServiceBusClient>();

        Assert.Equal(expectedEndpoint, client.FullyQualifiedNamespace);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", "testqueue")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithQueue_CreatesQueueReceiver(string connectionString, string? expectedQueueName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Check that settings are respected over value in in connection string
        builder.AddAzureServiceBusReceiver("sb", settings =>
        {
            if (expectedQueueName == null)
            {
                settings.QueueOrTopicName = "myqueue";
            }
        });

        using var host = builder.Build();
        var receiver = host.Services.GetRequiredService<ServiceBusReceiver>();

        var expectedName = expectedQueueName ?? "myqueue";
        Assert.Equal(expectedName, receiver.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", "testqueue")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithQueue_CreatesQueueSender(string connectionString, string? expectedQueueName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Check that settings are respected over value in in connection string
        builder.AddAzureServiceBusSender("sb", settings =>
        {
            if (expectedQueueName == null)
            {
                settings.QueueOrTopicName = "myqueue";
            }
        });

        using var host = builder.Build();
        var sender = host.Services.GetRequiredService<ServiceBusSender>();

        var expectedName = expectedQueueName ?? "myqueue";
        Assert.Equal(expectedName, sender.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic;SharedAccessKeyName=fake;SharedAccessKey=fake", "testtopic")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithTopic_CreatesTopicSender(string connectionString, string? expectedTopicName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Check that settings are respected over value in in connection string
        builder.AddAzureServiceBusSender("sb", settings =>
        {
            if (expectedTopicName == null)
            {
                settings.QueueOrTopicName = "mytopic";
            }
        });

        using var host = builder.Build();
        var sender = host.Services.GetRequiredService<ServiceBusSender>();

        var expectedName = expectedTopicName ?? "mytopic";
        Assert.Equal(expectedName, sender.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic/subscriptions/testsub;SharedAccessKeyName=fake;SharedAccessKey=fake", "testtopic", "testsub")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic;SharedAccessKeyName=fake;SharedAccessKey=fake", null, null)]
    public void ConnectionString_WithTopicAndSubscription_CreatesSubscriptionReceiver(string connectionString, string? expectedTopicName, string? expectedSubscriptionName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Check that settings are respected over value in in connection string
        builder.AddAzureServiceBusReceiver("sb", settings =>
        {
            if (expectedTopicName == null)
            {
                settings.QueueOrTopicName = "mytopic";
                settings.SubscriptionName = "mysub";
            }
        });

        using var host = builder.Build();
        var receiver = host.Services.GetRequiredService<ServiceBusReceiver>();

        var topicName = expectedTopicName ?? "mytopic";
        var subscriptionName = expectedSubscriptionName ?? "mysub";
        Assert.Equal($"{topicName}/Subscriptions/{subscriptionName}", receiver.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", "testqueue")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithQueue_CreatesQueueProcessor(string connectionString, string? expectedQueueName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Check that settings are respected over value in in connection string
        builder.AddAzureServiceBusProcessor("sb",
            settings =>
            {
                if (expectedQueueName == null)
                {
                    settings.QueueOrTopicName = "myqueue";
                }
            },
            builder =>
            {
                builder.ConfigureOptions(o =>
                {
                    o.MaxConcurrentCalls = 5;
                    o.AutoCompleteMessages = false;
                });
            });

        using var host = builder.Build();
        var processor = host.Services.GetRequiredService<ServiceBusProcessor>();

        var expectedName = expectedQueueName ?? "myqueue";
        Assert.Equal(expectedName, processor.EntityPath);
        Assert.Equal(5, processor.MaxConcurrentCalls);
        Assert.False(processor.AutoCompleteMessages);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic/subscriptions/testsub;SharedAccessKeyName=fake;SharedAccessKey=fake", "testtopic", "testsub")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic;SharedAccessKeyName=fake;SharedAccessKey=fake", null, null)]
    public void ConnectionString_WithTopicAndSubscription_CreatesSubscriptionProcessor(string connectionString, string? expectedTopicName, string? expectedSubscriptionName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        var options = new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 3,
            AutoCompleteMessages = true
        };

        // If not explicitly in connection string, set it in settings
        builder.AddAzureServiceBusProcessor("sb",
            settings =>
            {
                if (expectedTopicName == null)
                {
                    settings.QueueOrTopicName = "mytopic";
                    settings.SubscriptionName = "mysub";
                }
            },
            builder =>
            {
                builder.ConfigureOptions(o =>
                {
                    o.MaxConcurrentCalls = 3;
                    o.AutoCompleteMessages = true;
                });
            });

        using var host = builder.Build();
        var processor = host.Services.GetRequiredService<ServiceBusProcessor>();

        var topicName = expectedTopicName ?? "mytopic";
        var subscriptionName = expectedSubscriptionName ?? "mysub";
        Assert.Equal($"{topicName}/Subscriptions/{subscriptionName}", processor.EntityPath);
        Assert.Equal(3, processor.MaxConcurrentCalls);
        Assert.True(processor.AutoCompleteMessages);
    }

    [Fact]
    public void ConnectionString_WithoutEntityPath_ThrowsException_ForSender()
    {
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusSender("sb");
        using var host = builder.Build();

        Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<ServiceBusSender>);
    }

    [Fact]
    public void ConnectionString_WithoutEntityPath_ThrowsException_ForReceiver()
    {
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusReceiver("sb");
        using var host = builder.Build();

        Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<ServiceBusReceiver>);
    }

    [Fact]
    public void ConnectionString_WithoutEntityPath_ThrowsException_ForProcessor()
    {
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusProcessor("sb");
        using var host = builder.Build();

        Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<ServiceBusProcessor>);
    }

    [Fact]
    public void Keyed_ConnectionString_WithQueue_CreatesQueueReceiver()
    {
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddKeyedAzureServiceBusReceiver("sb");

        using var host = builder.Build();
        var receiver = host.Services.GetRequiredKeyedService<ServiceBusReceiver>("sb");

        Assert.Equal("testqueue", receiver.EntityPath);
    }

    [Fact]
    public void MultipleServiceBusComponentsCanBeRegistered()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        // Two different queues
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue1", "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=queue1;SharedAccessKeyName=fake;SharedAccessKey=fake"),
            new KeyValuePair<string, string?>("ConnectionStrings:queue2", "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=queue2;SharedAccessKeyName=fake;SharedAccessKey=fake"),
        ]);

        // One mapped to a sender and one mapped to a receiver
        builder.AddAzureServiceBusSender("queue1");
        builder.AddAzureServiceBusReceiver("queue2");

        using var host = builder.Build();
        var sender = host.Services.GetRequiredService<ServiceBusSender>();
        var receiver = host.Services.GetRequiredService<ServiceBusReceiver>();

        Assert.Equal("queue1", sender.EntityPath);
        Assert.Equal("queue2", receiver.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", "testqueue")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithQueueEntityPath_CreatesQueueReceiver(string connectionString, string? expectedQueueName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Can consume queue name from settings if not in entity path
        builder.AddAzureServiceBusReceiver("sb", settings =>
        {
            if (expectedQueueName == null)
            {
                settings.QueueOrTopicName = "myqueue";
            }
        });

        using var host = builder.Build();
        var receiver = host.Services.GetRequiredService<ServiceBusReceiver>();

        var expectedName = expectedQueueName ?? "myqueue";
        Assert.Equal(expectedName, receiver.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", "testqueue")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithQueueEntityPath_CreatesQueueSender(string connectionString, string? expectedQueueName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Can consume queue name from settings if not in entity path
        builder.AddAzureServiceBusSender("sb", settings =>
        {
            if (expectedQueueName == null)
            {
                settings.QueueOrTopicName = "myqueue";
            }
        });

        using var host = builder.Build();
        var sender = host.Services.GetRequiredService<ServiceBusSender>();

        var expectedName = expectedQueueName ?? "myqueue";
        Assert.Equal(expectedName, sender.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic;SharedAccessKeyName=fake;SharedAccessKey=fake", "testtopic")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithTopicEntityPath_CreatesTopicSender(string connectionString, string? expectedTopicName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Can consume topic name from settings if not in entity path
        builder.AddAzureServiceBusSender("sb", settings =>
        {
            if (expectedTopicName == null)
            {
                settings.QueueOrTopicName = "mytopic";
            }
        });

        using var host = builder.Build();
        var sender = host.Services.GetRequiredService<ServiceBusSender>();

        var expectedName = expectedTopicName ?? "mytopic";
        Assert.Equal(expectedName, sender.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic/Subscriptions/testsub;SharedAccessKeyName=fake;SharedAccessKey=fake", "testtopic", "testsub")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake", null, null)]
    public void ConnectionString_WithSubscriptionEntityPath_CreatesSubscriptionReceiver(string connectionString, string? expectedTopicName, string? expectedSubscriptionName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Can consume topic name from settings if not in entity path
        builder.AddAzureServiceBusReceiver("sb", settings =>
        {
            if (expectedTopicName == null)
            {
                settings.QueueOrTopicName = "mytopic";
                settings.SubscriptionName = "mysub";
            }
        });

        using var host = builder.Build();
        var receiver = host.Services.GetRequiredService<ServiceBusReceiver>();

        var topicName = expectedTopicName ?? "mytopic";
        var subscriptionName = expectedSubscriptionName ?? "mysub";
        Assert.Equal($"{topicName}/Subscriptions/{subscriptionName}", receiver.EntityPath);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake", "testqueue")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake", null)]
    public void ConnectionString_WithQueueEntityPath_CreatesQueueProcessor(string connectionString, string? expectedQueueName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Can consume queue name from settings if not in entity path
        builder.AddAzureServiceBusProcessor("sb",
            settings =>
            {
                if (expectedQueueName == null)
                {
                    settings.QueueOrTopicName = "myqueue";
                }
            },
            builder =>
            {
                builder.ConfigureOptions(o =>
                {
                    o.MaxConcurrentCalls = 5;
                    o.AutoCompleteMessages = false;
                });
            });

        using var host = builder.Build();
        var processor = host.Services.GetRequiredService<ServiceBusProcessor>();

        var expectedName = expectedQueueName ?? "myqueue";
        Assert.Equal(expectedName, processor.EntityPath);
        Assert.Equal(5, processor.MaxConcurrentCalls);
        Assert.False(processor.AutoCompleteMessages);
    }

    [Theory]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testtopic/Subscriptions/testsub;SharedAccessKeyName=fake;SharedAccessKey=fake", "testtopic", "testsub")]
    [InlineData("Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake", null, null)]
    public void ConnectionString_WithSubscriptionEntityPath_CreatesSubscriptionProcessor(string connectionString, string? expectedTopicName, string? expectedSubscriptionName)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Can consume topic name from settings if not in entity path
        builder.AddAzureServiceBusProcessor("sb",
            settings =>
            {
                if (expectedTopicName == null)
                {
                    settings.QueueOrTopicName = "mytopic";
                    settings.SubscriptionName = "mysub";
                }
            },
            builder =>
            {
                builder.ConfigureOptions(o =>
                {
                    o.MaxConcurrentCalls = 3;
                    o.AutoCompleteMessages = true;
                });
            });

        using var host = builder.Build();
        var processor = host.Services.GetRequiredService<ServiceBusProcessor>();

        var topicName = expectedTopicName ?? "mytopic";
        var subscriptionName = expectedSubscriptionName ?? "mysub";
        Assert.Equal($"{topicName}/Subscriptions/{subscriptionName}", processor.EntityPath);
        Assert.Equal(3, processor.MaxConcurrentCalls);
        Assert.True(processor.AutoCompleteMessages);
    }

    [Fact]
    public void Keyed_ConnectionString_WithEntityPath_CreatesQueueReceiver()
    {
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=testqueue;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddKeyedAzureServiceBusReceiver("sb");

        using var host = builder.Build();
        var receiver = host.Services.GetRequiredKeyedService<ServiceBusReceiver>("sb");

        Assert.Equal("testqueue", receiver.EntityPath);
    }

    [Fact]
    public void Multiple_ServiceBus_Components_CanBeRegistered_With_EntityPath()
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);

        // Setup with two different queues using EntityPath
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:queue1", "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=queue1;SharedAccessKeyName=fake;SharedAccessKey=fake"),
            new KeyValuePair<string, string?>("ConnectionStrings:queue2", "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=queue2;SharedAccessKeyName=fake;SharedAccessKey=fake"),
        ]);

        // Add a sender for first queue and receiver for second queue
        builder.AddAzureServiceBusSender("queue1");
        builder.AddAzureServiceBusReceiver("queue2");

        using var host = builder.Build();
        var sender = host.Services.GetRequiredService<ServiceBusSender>();
        var receiver = host.Services.GetRequiredService<ServiceBusReceiver>();

        Assert.Equal("queue1", sender.EntityPath);
        Assert.Equal("queue2", receiver.EntityPath);
    }

    [Fact]
    public void PrefersSettingsOverConnectionStringForCustomizations()
    {
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=connectionqueue;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusReceiver("sb", settings =>
        {
            settings.QueueOrTopicName = "settingsqueue";
        });

        using var host = builder.Build();
        var receiver = host.Services.GetRequiredService<ServiceBusReceiver>();

        // EntityPath from settings takes precedence over connection string
        Assert.Equal("settingsqueue", receiver.EntityPath);
    }

    [Fact]
    public void EntityPath_SubscriptionFormat_CorrectlyParsed()
    {
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;EntityPath=mytopic/Subscriptions/mysubscription;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        PopulateConfiguration(builder.Configuration, connectionString);

        // Create both a sender (should use topic name) and a processor (with topic and subscription)
        builder.AddAzureServiceBusSender("sb");
        builder.AddAzureServiceBusProcessor("sb");

        using var host = builder.Build();
        var sender = host.Services.GetRequiredService<ServiceBusSender>();
        var processor = host.Services.GetRequiredService<ServiceBusProcessor>();

        Assert.Equal("mytopic", sender.EntityPath);
        Assert.Equal("mytopic/Subscriptions/mysubscription", processor.EntityPath);
    }

    [Fact]
    public void ReceiverThrowsIfRequiredEntityPathMissing()
    {
        // Arrange
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = "sb";
        var configurationSectionName = "Aspire:Azure:Messaging:ServiceBus";
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusReceiver(connectionName, settings =>
        {
            settings.QueueOrTopicName = null;
            settings.SubscriptionName = null;
        });

        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<ServiceBusReceiver>);
        Assert.Equal($"A ServiceBusReceiver could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'QueueOrTopicName' and, if using a subscription, 'SubscriptionName' in the '{configurationSectionName}' configuration section.", exception.Message);
    }

    [Fact]
    public void SenderThrowsIfRequiredEntityPathMissing()
    {
        // Arrange
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = "sb";
        var configurationSectionName = "Aspire:Azure:Messaging:ServiceBus";
        PopulateConfiguration(builder.Configuration, connectionString);

        builder.AddAzureServiceBusSender("sb", settings =>
        {
            settings.QueueOrTopicName = null;
        });

        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<ServiceBusSender>);
        Assert.Equal($"A ServiceBusSender could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'QueueOrTopicName' in the '{configurationSectionName}' configuration section.", exception.Message);
    }

    [Fact]
    public void ProcessorThrowsIfRequiredEntityPathMissing()
    {
        // Arrange
        var connectionString = "Endpoint=sb://aspireservicebustests.servicebus.windows.net;SharedAccessKeyName=fake;SharedAccessKey=fake";
        var builder = Host.CreateEmptyApplicationBuilder(null);
        var connectionName = "sb";
        var configurationSectionName = "Aspire:Azure:Messaging:ServiceBus";
        PopulateConfiguration(builder.Configuration, connectionString);

        // Act - Configure with the test parameters
        builder.AddAzureServiceBusProcessor("sb", settings =>
        {
            settings.QueueOrTopicName = null;
            settings.SubscriptionName = null;
        });

        using var host = builder.Build();

        var exception = Assert.Throws<InvalidOperationException>(host.Services.GetRequiredService<ServiceBusProcessor>);
        Assert.Equal($"A ServiceBusProcessor could not be configured. Ensure valid connection information was provided in 'ConnectionStrings:{connectionName}' or specify a 'QueueOrTopicName' and, if using a subscription, 'SubscriptionName' in the '{configurationSectionName}' configuration section.", exception.Message);
    }

    private static void PopulateConfiguration(ConfigurationManager configuration, string connectionString) =>
    configuration.AddInMemoryCollection([
        new KeyValuePair<string, string?>("ConnectionStrings:sb", connectionString)
    ]);
}
