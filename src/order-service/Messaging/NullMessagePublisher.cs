namespace OrderService.Messaging;

public class NullMessagePublisher : IMessagePublisher
{
    public Task PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default)
    {
        // No-op when Service Bus is not configured
        return Task.CompletedTask;
    }
}
