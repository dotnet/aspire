// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Azure.Common;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace Aspire.Azure.Messaging.ServiceBus.Tests;

public class AzureMessagingServiceBusSettingsTests
{
    [Fact]
    public void TracingIsEnabledWhenAzureSwitchIsSet()
    {
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(true)).Dispose();
        RemoteExecutor.Invoke(() => EnsureTracingIsEnabledWhenAzureSwitchIsSet(false), ConformanceTests.EnableTracingForAzureSdk()).Dispose();
    }

    private static void EnsureTracingIsEnabledWhenAzureSwitchIsSet(bool expectedValue)
    {
        Assert.Equal(expectedValue, new AzureMessagingServiceBusSettings().DisableTracing);
    }

    [Theory]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=keyvalue")]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key+with+chars")]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=keyvalue;")]
    public void ParseConnectionString_PreservesOriginalFormatWhenNoEntityPath(string connectionString)
    {
        // Regression test for issue #9448: Ensure connection string format is preserved exactly
        // when no EntityPath is present. This tests the fix that prevents DbConnectionStringBuilder
        // from normalizing the connection string (converting to lowercase, adding quotes, etc.)
        var settings = new AzureMessagingServiceBusSettings();
        
        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);
        
        Assert.Equal(connectionString, settings.ConnectionString);
        Assert.Null(settings.QueueOrTopicName);
        Assert.Null(settings.SubscriptionName);
    }

    [Theory]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=keyvalue;EntityPath=myqueue", 
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=keyvalue", 
                "myqueue", null)]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key+with+chars;EntityPath=mytopic", 
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=key+with+chars", 
                "mytopic", null)]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=keyvalue;EntityPath=mytopic/Subscriptions/mysub", 
                "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=keyvalue", 
                "mytopic", "mysub")]
    public void ParseConnectionString_PreservesOriginalFormatWhenEntityPathPresent(string connectionString, string expectedConnectionString, string expectedQueueOrTopic, string? expectedSubscription)
    {
        // Regression test for issue #9448: Ensure connection string format is preserved exactly
        // when EntityPath is present, but EntityPath is removed. This is the key fix - we extract
        // the EntityPath value for parsing but preserve the original connection string format.
        var settings = new AzureMessagingServiceBusSettings();
        
        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);
        
        Assert.Equal(expectedConnectionString, settings.ConnectionString);
        Assert.Equal(expectedQueueOrTopic, settings.QueueOrTopicName);
        Assert.Equal(expectedSubscription, settings.SubscriptionName);
    }

    [Theory]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;EntityPath=myqueue")]
    [InlineData("Endpoint=sb://test.servicebus.windows.net/;EntityPath=mytopic/Subscriptions/mysub")]
    public void ParseConnectionString_SetsFullyQualifiedNamespaceWhenOnlyEndpointRemains(string connectionString)
    {
        // Test the case where after removing EntityPath, only Endpoint remains,
        // so we should set FullyQualifiedNamespace instead of ConnectionString
        var settings = new AzureMessagingServiceBusSettings();
        
        ((IConnectionStringSettings)settings).ParseConnectionString(connectionString);
        
        Assert.Equal("test.servicebus.windows.net", settings.FullyQualifiedNamespace);
        Assert.Null(settings.ConnectionString);
    }
}
