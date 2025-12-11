using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace OrderService.Messaging;

public class ProductEventConsumer : BackgroundService
{
    private readonly ServiceBusClient _client;
    private readonly ServiceBusProcessor _processor;
    private readonly ILogger<ProductEventConsumer> _logger;

    public ProductEventConsumer(string connectionString, string queueName, ILogger<ProductEventConsumer> logger)
    {
        _logger = logger;
        _client = new ServiceBusClient(connectionString);
        _processor = _client.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            AutoCompleteMessages = false,
            MaxConcurrentCalls = 1
        });

        _processor.ProcessMessageAsync += ProcessMessageAsync;
        _processor.ProcessErrorAsync += ProcessErrorAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product Event Consumer starting...");
        await _processor.StartProcessingAsync(stoppingToken);
        _logger.LogInformation("Product Event Consumer started successfully");
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var messageBody = args.Message.Body.ToString();
        var messageType = args.Message.Subject;

        _logger.LogInformation("Received message: Type={MessageType}, Body={MessageBody}", messageType, messageBody);

        try
        {
            switch (messageType)
            {
                case nameof(ProductCreatedEvent):
                    var createdEvent = JsonSerializer.Deserialize<ProductCreatedEvent>(messageBody);
                    _logger.LogInformation("Product Created: ID={ProductId}, Name={ProductName}, Price={Price}", 
                        createdEvent?.ProductId, createdEvent?.Name, createdEvent?.Price);
                    break;

                case nameof(ProductUpdatedEvent):
                    var updatedEvent = JsonSerializer.Deserialize<ProductUpdatedEvent>(messageBody);
                    _logger.LogInformation("Product Updated: ID={ProductId}, Name={ProductName}, Price={Price}", 
                        updatedEvent?.ProductId, updatedEvent?.Name, updatedEvent?.Price);
                    break;

                case nameof(ProductDeletedEvent):
                    var deletedEvent = JsonSerializer.Deserialize<ProductDeletedEvent>(messageBody);
                    _logger.LogInformation("Product Deleted: ID={ProductId}", deletedEvent?.ProductId);
                    break;

                default:
                    _logger.LogWarning("Unknown message type: {MessageType}", messageType);
                    break;
            }

            // Mark message as complete
            await args.CompleteMessageAsync(args.Message);
            _logger.LogInformation("Message processed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
            // Abandon the message so it can be retried
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ProcessErrorAsync(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Error in Product Event Consumer: {ErrorSource}", args.ErrorSource);
        return Task.CompletedTask;
    }

    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Product Event Consumer stopping...");
        await _processor.StopProcessingAsync(stoppingToken);
        await _processor.DisposeAsync();
        await _client.DisposeAsync();
        _logger.LogInformation("Product Event Consumer stopped");
    }

    // Product event classes for deserialization
    private class ProductCreatedEvent
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    private class ProductUpdatedEvent
    {
        public int ProductId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    private class ProductDeletedEvent
    {
        public int ProductId { get; set; }
        public DateTime DeletedAt { get; set; }
    }
}
