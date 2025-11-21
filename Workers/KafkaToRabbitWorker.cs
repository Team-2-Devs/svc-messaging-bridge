using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Messaging.MessageContracts;
using Messaging.RabbitMQ;
using Messaging.Kafka;

namespace MessagingBridge.Workers;

public sealed class KafkaToRabbitWorker : BackgroundService
{
    private readonly IMessageConsumer _consumer;
    private readonly IEventPublisher _publisher;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public KafkaToRabbitWorker(IMessageConsumer consumer, IEventPublisher publisher)
    {
        _consumer = consumer;
        _publisher = publisher;
    }

    protected override Task ExecuteAsync(CancellationToken ct)
    {
        _consumer.Subscribe(Topics.ImageUploaded, Topics.RecognitionCompleted);
        System.Console.WriteLine("Subscribed to ImageUploaded and RecognitionCompleted");
        return _consumer.RunAsync(HandleAsync, ct);
    }

    private async Task HandleAsync(string topic, string key, string value)
    {
        if (topic == Topics.ImageUploaded)
        {
            var evt = JsonSerializer.Deserialize<ImageUploaded>(value, JsonOpts);
            if (evt is null) return;
            System.Console.WriteLine("Received ImageUploaded");

            // Forward to RabbitMQ (fanout)
            await _publisher.PublishAsync(Exchanges.ImageUploaded, "", evt, ExchangeKind.Fanout, ct: default);
        };

        if (topic == Topics.RecognitionCompleted)
        {
            var evt = JsonSerializer.Deserialize<RecognitionCompleted>(value, JsonOpts);
            if (evt is null) return;

            System.Console.WriteLine("Received RecognitionCompleted");

            // Forward to RabbitMQ (fanout)
            await _publisher.PublishAsync(Exchanges.RecognitionCompleted, "", evt, ExchangeKind.Fanout, ct: default);
        };
    }
}