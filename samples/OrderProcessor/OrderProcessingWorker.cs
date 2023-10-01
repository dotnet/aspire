using System.Diagnostics;
using Azure.Messaging.ServiceBus;
using BasketService.Models;

namespace OrderProcessor;

public class OrderProcessingWorker : BackgroundService
{
    public static readonly string ActivitySourceName = "Worker";

    public static ActivitySource ActivitySource { get; } = new ActivitySource(ActivitySourceName);

    private readonly ILogger<OrderProcessingWorker> _logger;
    private readonly IConfiguration _config;
    private readonly ServiceBusClient? _client;
    private ServiceBusProcessor? _messageProcessor;

    public OrderProcessingWorker(ILogger<OrderProcessingWorker> logger, IConfiguration config, ServiceBusClient? client = null)
    {
        _logger = logger;
        _config = config;
        _client = client;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_client is null)
        {
            _logger.LogCritical($"Azure ServiceBus is unavailable. Ensure you have configured it - for example in MyApp's user secrets under '{AspireServiceBusExtensions.DefaultNamespaceConfigKey}'.");
            return;
        }

        const string configKeyName = "Aspire:Azure:Messaging:ServiceBus:OrderQueueName";
        string queueName = _config[configKeyName] ?? "orders";

        _messageProcessor = _client.CreateProcessor(queueName);
        _messageProcessor.ProcessMessageAsync += ProcessMessageAsync;
        _messageProcessor.ProcessErrorAsync += ProcessErrorAsync;
        await _messageProcessor.StartProcessingAsync(stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_messageProcessor is not null)
        {
            await _messageProcessor.StopProcessingAsync(cancellationToken);

            await _messageProcessor.DisposeAsync();
        }
    }

    private Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        using (var activity = ActivitySource.StartActivity("order-processor.worker"))
        {
            _logger.LogInformation($"Processing Order at: {DateTime.UtcNow}");

            var message = args.Message;

            if (_logger.Equals(LogLevel.Debug))
            {
                _logger.LogDebug("""
                MessageId:{MessageId}
                MessageBody:{Body}
                """, message.MessageId, message.Body);
            }
            var order = message.Body.ToObjectFromJson<Order>();

            activity?.AddTag("order-id", order.Id);
            activity?.AddTag("product-count", order.Items.Count);

            _logger.LogInformation("""
                OrderId:{Id}
                BuyerId:{BuyerId}
                ProductCount:{Count}
                """, order.Id, order.BuyerId, order.Items.Count);

            return Task.CompletedTask;
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs arg)
    {
        _logger.LogError(arg.Exception, "Error processing a message. ErrorSource={errorSource}, EntityPath={entityPath}.", arg.ErrorSource, arg.EntityPath);
        return Task.CompletedTask;
    }
}
