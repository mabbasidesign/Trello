using Azure.Messaging.ServiceBus;
using System.Text.Json;

namespace ProductService.Messaging;

public class ServiceBusPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _client;
    private readonly ILogger<ServiceBusPublisher> _logger;

    public ServiceBusPublisher(string connectionString, ILogger<ServiceBusPublisher> logger)
    {
        _client = new ServiceBusClient(connectionString);
        _logger = logger;
    }

    public async Task PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default)
    {
        try
        {
            var sender = _client.CreateSender(queueOrTopicName);
            var messageBody = JsonSerializer.Serialize(message);
            var serviceBusMessage = new ServiceBusMessage(messageBody)
            {
                ContentType = "application/json",
                Subject = typeof(T).Name
            };

            await sender.SendMessageAsync(serviceBusMessage, cancellationToken);
            
            _logger.LogInformation("Published message to {QueueOrTopic}: {MessageType}", 
                queueOrTopicName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to {QueueOrTopic}", queueOrTopicName);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _client.DisposeAsync();
    }
}
