namespace ProductService.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string queueOrTopicName, T message, CancellationToken cancellationToken = default);
}
