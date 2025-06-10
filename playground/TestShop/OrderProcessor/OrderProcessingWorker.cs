using System.Diagnostics;
using System.Text.Json;
using BasketService.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace OrderProcessor;

public class OrderProcessingWorker : BackgroundService
{
    private readonly ILogger<OrderProcessingWorker> _logger;
    private readonly IConfiguration _config;
    private readonly IServiceProvider _serviceProvider;
    private IConnection? _messageConnection;
    private IChannel? _messageChannel;

    public OrderProcessingWorker(ILogger<OrderProcessingWorker> logger, IConfiguration config, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _config = config;
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Factory.StartNew(async () =>
        {
            const string configKeyName = "Aspire:RabbitMQ:Client:OrderQueueName";
            string queueName = _config[configKeyName] ?? "orders";

            _messageConnection = _serviceProvider.GetRequiredService<IConnection>();

            _messageChannel = await _messageConnection.CreateChannelAsync();
            await _messageChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false);

            var consumer = new AsyncEventingBasicConsumer(_messageChannel);
            consumer.ReceivedAsync += ProcessMessageAsync;

            await _messageChannel.BasicConsumeAsync(
                queueName,
                autoAck: true,
                consumer);
        }, stoppingToken, TaskCreationOptions.LongRunning, TaskScheduler.Current);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
       await base.StopAsync(cancellationToken);

        _messageChannel?.Dispose();
    }

    private Task ProcessMessageAsync(object? sender, BasicDeliverEventArgs args)
    {
        _logger.LogInformation($"Processing Order at: {DateTime.UtcNow}");

        var message = args.Body;

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("""
                MessageId:{MessageId}
                MessageBody:{Body}
                """, args.BasicProperties.MessageId, message);
        }
        var order = JsonSerializer.Deserialize<Order>(message.Span) ?? new Order() { Id = "fake" };

        Activity.Current?.AddTag("order-id", order.Id);
        Activity.Current?.AddTag("product-count", order.Items.Count);

        _logger.LogInformation("""
            OrderId:{Id}
            BuyerId:{BuyerId}
            ProductCount:{Count}
            """, order.Id, order.BuyerId, order.Items.Count);

        return Task.CompletedTask;
    }
}
